using System.Text;
using System.Text.RegularExpressions;
using Instruction = Shared.GameFormats.MusicDat.MusicDatDisassembler.Instruction;

namespace Shared.GameFormats.MusicDat
{
    /// <summary>
    /// Renders a disassembled function as structured pseudocode, so a reader sees
    /// <c>if (Feedback_Subculture_Gameplay == "wh2_main_sc_skv_skaven")</c> instead of a
    /// column of opcodes and slot numbers.
    ///
    /// Two things are recovered:
    ///
    /// EXPRESSIONS. The VM is a stack machine whose slot operands are the compiler's
    /// static stack offsets, so they are not sequential and cannot be predicted by a
    /// counter. They do not need to be: a value-producing instruction never names its own
    /// destination, but its consumer names it, and producers are consumed in emission
    /// order. So each producer is left "pending" until a consumer appears, and the trailing
    /// pending producers are bound to that consumer's trailing slot operands. Operands not
    /// covered by a pending producer are looked up in the slot map, which keeps values
    /// alive across branches (an else-if chain loads the tested variable once and re-reads
    /// its slot in every arm).
    ///
    /// A function call whose result is used breaks the simple "trailing" rule: the
    /// compiler pushes a placeholder for the eventual return value right after the real
    /// arguments, so the last pending value at bind time is the placeholder, not the
    /// final argument. Context.BindArg resolves this by matching an argument bind to a
    /// pending value of the right type (a string argument is never the int placeholder)
    /// rather than by position, and Context.EmitCallOrExpr claims whatever placeholder is
    /// left once the call itself runs, folding "Foo(); if (slotN)" into "if (Foo())".
    ///
    /// CONTROL FLOW. JUMP_IF_FALSE spans an if-body; a trailing forward JUMP over a
    /// following region makes it an if/else. JUMP_3 (a switch/case chain's per-case test)
    /// gets the same if-body treatment with an unnamed placeholder condition, since the
    /// code between it and its target is that case's real, reachable body rather than dead
    /// code after an unconditional jump. Anything that does not fit either shape is emitted
    /// as an explicit <c>goto</c> to a labelled address rather than guessed at, so the
    /// output never claims structure the bytecode does not have. A jump to a run of slot
    /// bookkeeping ending in RETURN is rendered as <c>return</c>, which is what the shared
    /// epilogue of an else-if chain actually is.
    ///
    /// This is a reading aid only - nothing here is used to write files back. Unknown
    /// opcodes and unresolved slots degrade to a raw rendering instead of throwing.
    /// </summary>
    public static class MusicDatDecompiler
    {
        /// <summary>One rendered line. <see cref="Address"/> is the instruction it came
        /// from (-1 for structural lines like a closing brace), which lets a UI line the
        /// pseudocode up against the raw instruction list.</summary>
        public record Line(int Address, int Indent, string Text);

        /// <summary><see cref="IsComplete"/> is false when a value could not be traced back to
        /// what produced it, which happens where an unidentified opcode produces a result the
        /// dataflow cannot follow through. Those lines show a bare <c>slotN</c>. Callers
        /// should say so rather than present the rendering as authoritative.</summary>
        public record DecompiledFunction(IReadOnlyList<Line> Lines, bool IsComplete);

        public static string ToText(MusicDatFile file, MusicDatDisassembler.Function function)
        {
            var result = Decompile(file, function);
            var sb = new StringBuilder();

            if (!result.IsComplete)
                sb.AppendLine("// Partial: some values below could not be traced to what produced them (shown as slotN).");

            foreach (var line in result.Lines)
                sb.Append(new string(' ', line.Indent * 4)).AppendLine(line.Text);
            return sb.ToString();
        }

        public static DecompiledFunction Decompile(MusicDatFile file, MusicDatDisassembler.Function function)
        {
            var ctx = new Context(file, function);
            ctx.EmitSignature();
            ctx.EmitRange(0, function.Instructions.Count, 1);
            ctx.Emit(-1, 0, "}");
            var lines = MergeAdjacentIdenticalIfChains(ctx.Finish());

            // Repeated because a goto can land on an arm that is itself only a label and a
            // second goto (battle Wwise_cues: "L26260: goto L25302;"). That arm isn't
            // goto-only until the label is found unreferenced and dropped, so a single pass
            // leaves the second goto in place. Capped in case a short target ever jumps back
            // into itself, which would otherwise grow on every pass.
            for (var pass = 0; pass < MaxInlinePasses; pass++)
            {
                var next = RemoveUnreferencedLabels(InlineShortGotoTargets(lines));
                var changed = next.Count != lines.Count || !next.Select(l => l.Text).SequenceEqual(lines.Select(l => l.Text));
                lines = next;
                if (!changed)
                    break;
            }
            return new DecompiledFunction(lines, ctx.IsComplete);
        }

        /// <summary>Folds a run of <em>consecutive</em>, brace-delimited <c>if</c> blocks
        /// with no <c>else</c> into one <c>if (a || b || ...)</c> when their bodies render
        /// as the exact same lines - the shape a dispatch function's else-if chain takes
        /// when several adjacent keys are handled identically. Deliberately stops at the
        /// first arm that doesn't match rather than hopping over it to find a later match
        /// further down the chain: even where that would be provably safe (mutually
        /// exclusive conditions on the same variable), a reader seeing several conditions
        /// OR'd together reasonably expects them to be the only thing separating this
        /// branch from its neighbours, and reaching over a distinct arm to merge two
        /// non-adjacent ones would misrepresent that there was a branch-off in between.
        ///
        /// An arm that is nothing but <c>goto L;</c> is compared as if it held the body of
        /// whichever immediately-adjacent arm's block that <c>L</c> labels (label line
        /// stripped) - the two are the same destination even though only one spells out the
        /// code. See <see cref="InlineShortGotoTargets"/> for the separate pass that
        /// replaces a short enough <c>goto</c> with its target's code even when it wasn't
        /// adjacent enough to merge here.</summary>
        static List<Line> MergeAdjacentIdenticalIfChains(List<Line> lines)
        {
            var result = new List<Line>();
            var i = 0;
            while (i < lines.Count)
            {
                if (!TryReadIfBlock(lines, i, out var header, out var body, out var hasElse, out var afterBlock))
                {
                    result.Add(lines[i]);
                    i++;
                    continue;
                }

                // Bodies are compared before being merged themselves, since a nested chain
                // only reads as "identical" arm-to-arm once it's already in its own most
                // reduced form.
                body = MergeAdjacentIdenticalIfChains(body);

                // An if/else pair can't be folded into a sibling - the else's condition is
                // "none of the above", which a plain OR of conditions doesn't express - but
                // its own two branches still get the same treatment recursively so a
                // mergeable chain nested inside either one isn't left alone just because
                // the pair itself sits still.
                if (hasElse)
                {
                    var elseBody = MergeAdjacentIdenticalIfChains(FindBlockBody(lines, afterBlock + 1, out var afterElse));

                    result.Add(header);
                    result.Add(new Line(-1, header.Indent, "{"));
                    result.AddRange(body);
                    result.Add(new Line(-1, header.Indent, "}"));
                    result.Add(new Line(-1, header.Indent, "else"));
                    result.Add(new Line(-1, header.Indent, "{"));
                    result.AddRange(elseBody);
                    result.Add(new Line(-1, header.Indent, "}"));
                    i = afterElse;
                    continue;
                }

                // The whole consecutive run is gathered up front - not just this arm and
                // its immediate successor - so a goto's label can be resolved regardless of
                // whether the arm holding that label comes before or after it in the run;
                // only the actual merging below is restricted to strict adjacency.
                var arms = new List<Arm> { new(header, body) };
                var runEnd = afterBlock;
                while (TryReadIfBlock(lines, runEnd, out var nextHeader, out var nextBody, out var nextHasElse, out var nextAfter) && !nextHasElse)
                {
                    arms.Add(new Arm(nextHeader, MergeAdjacentIdenticalIfChains(nextBody)));
                    runEnd = nextAfter;
                }

                var byLabel = new Dictionary<string, List<Line>>();
                foreach (var arm in arms)
                {
                    var label = LabelOf(arm.Body);
                    if (label != null)
                        byLabel[label] = arm.Body[1..];
                }

                List<Line> Effective(List<Line> armBody)
                {
                    var ownLabel = LabelOf(armBody);
                    if (ownLabel != null)
                        return armBody[1..];
                    var target = GotoTargetOf(armBody);
                    return target != null && byLabel.TryGetValue(target, out var canonical) ? canonical : armBody;
                }

                var effectiveBody = Effective(arms[0].Body);
                var conditions = new List<string> { ConditionOf(arms[0].Header.Text) };
                var bodyOwner = arms[0].Body;
                var mergedCount = 1;
                while (mergedCount < arms.Count && BodiesMatch(Effective(arms[mergedCount].Body), effectiveBody))
                {
                    conditions.Add(ConditionOf(arms[mergedCount].Header.Text));
                    if (LabelOf(arms[mergedCount].Body) != null)
                        bodyOwner = arms[mergedCount].Body; // keep the label alive in the merged output
                    mergedCount++;
                }

                result.Add(header with { Text = $"if ({string.Join(" || ", conditions)})" });
                result.Add(new Line(-1, header.Indent, "{"));
                result.AddRange(bodyOwner);
                result.Add(new Line(-1, header.Indent, "}"));

                // Re-derive the resume point from how many arms actually merged, rather
                // than tracking it alongside the scan above, since TryReadIfBlock already
                // knows how to walk past an arm's brace-delimited body.
                i = afterBlock;
                for (var skipped = 1; skipped < mergedCount; skipped++)
                    TryReadIfBlock(lines, i, out _, out _, out _, out i);
            }
            return result;
        }

        record Arm(Line Header, List<Line> Body);

        static readonly Regex LabelLineRegex = new(@"^L(\d+):$", RegexOptions.Compiled);
        static readonly Regex GotoOnlyBodyRegex = new(@"^goto L(\d+);$", RegexOptions.Compiled);
        static readonly Regex GotoReferenceRegex = new(@"goto L(\d+);", RegexOptions.Compiled);

        static string? LabelOf(List<Line> body) =>
            body.Count > 0 ? LabelLineRegex.Match(body[0].Text) is { Success: true } m ? m.Groups[1].Value : null : null;

        static string? GotoTargetOf(List<Line> body) =>
            body.Count == 1 ? GotoOnlyBodyRegex.Match(body[0].Text) is { Success: true } m ? m.Groups[1].Value : null : null;

        /// <summary>Replaces a <c>goto L;</c> arm with a copy of <c>L</c>'s own code when
        /// that code is short (<see cref="MaxInlineGotoLines"/> lines or fewer) - long
        /// enough to read in place instead of following a jump, but short enough that
        /// duplicating it at every call site doesn't bloat the function. This is separate
        /// from - and runs after - <see cref="MergeAdjacentIdenticalIfChains"/>, so it also
        /// catches a <c>goto</c> that wasn't adjacent to the arm holding its target (an
        /// unrelated arm sitting between them, say) and so never got merged there. The
        /// label itself is left for <see cref="RemoveUnreferencedLabels"/> to drop once no
        /// <c>goto</c> anywhere still needs it.</summary>
        const int MaxInlineGotoLines = 5;
        const int MaxInlinePasses = 4;

        static List<Line> InlineShortGotoTargets(List<Line> lines)
        {
            var labelBodies = new Dictionary<string, (int BaseIndent, List<Line> Body)>();
            CollectLabelBodies(lines, labelBodies);
            return RewriteGotoArms(lines, labelBodies);
        }

        static void CollectLabelBodies(List<Line> lines, Dictionary<string, (int BaseIndent, List<Line> Body)> labelBodies)
        {
            var i = 0;
            while (i < lines.Count)
            {
                if (!TryReadIfBlock(lines, i, out var header, out var body, out var hasElse, out var afterBlock))
                {
                    i++;
                    continue;
                }

                var label = LabelOf(body);
                if (label != null)
                    labelBodies.TryAdd(label, (header.Indent + 1, body[1..]));
                CollectLabelBodies(body, labelBodies);

                if (hasElse)
                {
                    var elseBody = FindBlockBody(lines, afterBlock + 1, out var afterElse);
                    CollectLabelBodies(elseBody, labelBodies);
                    i = afterElse;
                    continue;
                }
                i = afterBlock;
            }
        }

        static List<Line> RewriteGotoArms(List<Line> lines, Dictionary<string, (int BaseIndent, List<Line> Body)> labelBodies)
        {
            var result = new List<Line>();
            var i = 0;
            while (i < lines.Count)
            {
                if (!TryReadIfBlock(lines, i, out var header, out var body, out var hasElse, out var afterBlock))
                {
                    result.Add(lines[i]);
                    i++;
                    continue;
                }

                var target = GotoTargetOf(body);
                var newBody = target != null && labelBodies.TryGetValue(target, out var entry) && entry.Body.Count <= MaxInlineGotoLines
                    ? RewriteGotoArms(Reindent(entry.Body, header.Indent + 1 - entry.BaseIndent), labelBodies)
                    : RewriteGotoArms(body, labelBodies);

                result.Add(header);
                result.Add(new Line(-1, header.Indent, "{"));
                result.AddRange(newBody);
                result.Add(new Line(-1, header.Indent, "}"));

                if (hasElse)
                {
                    var elseBody = FindBlockBody(lines, afterBlock + 1, out var afterElse);
                    result.Add(new Line(-1, header.Indent, "else"));
                    result.Add(new Line(-1, header.Indent, "{"));
                    result.AddRange(RewriteGotoArms(elseBody, labelBodies));
                    result.Add(new Line(-1, header.Indent, "}"));
                    i = afterElse;
                    continue;
                }
                i = afterBlock;
            }
            return result;
        }

        static List<Line> Reindent(List<Line> body, int delta) =>
            delta == 0 ? body : body.Select(l => l with { Indent = l.Indent + delta }).ToList();

        /// <summary>Drops a <c>L791:</c> label line once nothing in the function still
        /// <c>goto</c>s it - the common outcome once every referring arm has either been
        /// folded into the same merged <c>if</c> as the arm the label sat in, or had that
        /// arm's code inlined in its place.</summary>
        static List<Line> RemoveUnreferencedLabels(List<Line> lines)
        {
            var referenced = new HashSet<string>();
            foreach (var line in lines)
                foreach (Match m in GotoReferenceRegex.Matches(line.Text))
                    referenced.Add(m.Groups[1].Value);

            return lines.Where(l => LabelLineRegex.Match(l.Text) is not { Success: true } m || referenced.Contains(m.Groups[1].Value)).ToList();
        }

        /// <summary>Recognises <c>if (cond)\n{\n...\n}</c> starting at <paramref name="index"/> -
        /// the shape TryEmitIf produces - tracking nested braces to find the matching close
        /// rather than assuming a fixed body length. <paramref name="hasElse"/> reports
        /// whether an <c>else</c> immediately follows, without consuming it - the caller
        /// decides what that means for merging.</summary>
        static bool TryReadIfBlock(List<Line> lines, int index, out Line header, out List<Line> body, out bool hasElse, out int afterBlock)
        {
            header = default!;
            body = [];
            hasElse = false;
            afterBlock = index;

            if (index + 1 >= lines.Count || !lines[index].Text.StartsWith("if (", StringComparison.Ordinal) ||
                lines[index + 1] is not { Text: "{" } open || open.Indent != lines[index].Indent)
                return false;

            header = lines[index];
            body = FindBlockBody(lines, index + 1, out afterBlock);
            hasElse = afterBlock < lines.Count && lines[afterBlock].Text == "else";
            return true;
        }

        /// <summary>Returns the lines strictly between the <c>{</c> at
        /// <paramref name="openBraceIndex"/> and its matching <c>}</c>, tracking nested
        /// braces so an inner if-block's own braces don't end the outer one early.
        /// <paramref name="afterBlock"/> is the index right after that closing brace.</summary>
        static List<Line> FindBlockBody(List<Line> lines, int openBraceIndex, out int afterBlock)
        {
            var depth = 1;
            var j = openBraceIndex + 1;
            while (j < lines.Count && depth > 0)
            {
                if (lines[j].Text == "{") depth++;
                else if (lines[j].Text == "}") depth--;
                if (depth > 0) j++;
            }
            afterBlock = j + 1;
            return lines[(openBraceIndex + 1)..j];
        }

        static bool BodiesMatch(List<Line> a, List<Line> b) =>
            a.Count == b.Count && a.Select(l => l.Text).SequenceEqual(b.Select(l => l.Text));

        static string ConditionOf(string ifHeaderText) => ifHeaderText["if (".Length..^1];

        // Precedence for parenthesising; higher binds looser.
        const int PrecAtom = 0;
        const int PrecUnary = 1;
        const int PrecMul = 2;
        const int PrecAdd = 3;
        const int PrecCompare = 4;
        const int PrecAnd = 5;
        const int PrecOr = 6;

        // Distinguishes string-typed pending values from numeric/bool ones, which is what
        // lets BindArg pick the right one when a call's argument and its return-value
        // placeholder are pending at the same time (see BindArg's doc comment).
        enum ExprKind { Unknown, Numeric, String }

        record Expr(string Text, int Precedence, ExprKind Kind = ExprKind.Unknown);

        sealed class Context(MusicDatFile file, MusicDatDisassembler.Function function)
        {
            readonly List<Instruction> _body = function.Instructions;
            readonly Dictionary<int, int> _indexOfAddress =
                function.Instructions.Select((ins, i) => (ins.Address, i)).ToDictionary(x => x.Address, x => x.i);

            readonly Dictionary<int, Expr> _slots = [];
            readonly Dictionary<int, int> _slotEpoch = [];
            readonly List<Expr> _pending = [];
            readonly List<string> _callArgs = [];
            readonly List<Line> _lines = [];
            readonly HashSet<int> _labels = [];
            readonly int[] _parameterOffsets = ParameterOffsets(function.ParameterTypes);
            readonly int[] _returnOffsets = ReturnOffsets(file, function);
            int _epoch;

            /// <summary>The frame offsets a function writes its results back through, which
            /// live just below its parameters - so for a function taking no parameters the
            /// first is -4, and for one taking a single string (one unit) it is -5.
            ///
            /// <see cref="MusicDatFile.FunctionEntry.ReturnOrLocalTypes"/> is what says how
            /// many there are, and it lines up exactly: across both files every function
            /// declaring one entry touches exactly one slot below its parameters, every
            /// function declaring two touches exactly two, and functions declaring none
            /// touch none. The caller side of the same convention is the run of zeroed
            /// placeholders EmitCallOrExpr claims, and the two agree - update_Check_Battle_End_Status
            /// writes its "end battle" result to the deeper slot, which is the placeholder its
            /// caller tests second.</summary>
            static int[] ReturnOffsets(MusicDatFile file, MusicDatDisassembler.Function function)
            {
                var entry = file.Functions.FirstOrDefault(f => f.CodeOffset == function.Start);
                if (entry is null)
                    return [];

                var parameterSize = entry.ParameterTypes.Sum(t => SlotSize(t));
                return entry.ReturnOrLocalTypes.Select((_, k) => -(parameterSize + 4 * (k + 1))).ToArray();
            }

            /// <summary>The frame offset each declared parameter is read from.
            ///
            /// Slots are byte-like units rather than a flat index: a numeric value occupies
            /// four (every numeric slot in a function body is a multiple of four - see
            /// Check_Update_Marching, which runs 0, 4, 8, ... 156) while a string occupies
            /// one (the same function's two string slots are 160 and 161, adjacent). The
            /// caller lays parameters down so the LAST one sits closest to the frame
            /// pointer, so each parameter's offset is the negated total size of itself and
            /// everything declared after it.
            ///
            /// AMM_drone_RTPC_Updater(float, string) is the check that pins both halves at
            /// once: it opens by storing frame[-1] into a "..._rtpc_name" string variable
            /// and frame[-5] into a "..._rtpc_value" float variable, so the string parameter
            /// (declared second) is at -1 and the float (declared first) at -(1+4). Assuming
            /// a flat one-unit stride instead had the effect of numbering parameters
            /// backwards - arg1 and arg2 swapped in every string function - and of not
            /// recognising numeric parameters as parameters at all, leaving Update(float)'s
            /// own argument rendered as a bare frame[-4].</summary>
            static int[] ParameterOffsets(List<uint> parameterTypes)
            {
                var offsets = new int[parameterTypes.Count];
                var runningSize = 0;
                for (var i = parameterTypes.Count - 1; i >= 0; i--)
                {
                    runningSize += SlotSize(parameterTypes[i]);
                    offsets[i] = -runningSize;
                }
                return offsets;
            }

            static int SlotSize(uint type) => type == 2 ? 1 : 4; // 2 = string; bool/float take four

            public void Emit(int address, int indent, string text) => _lines.Add(new Line(address, indent, text));

            public void EmitSignature()
            {
                var args = string.Join(", ", function.ParameterTypes.Select((t, i) => $"{TypeName(t)} arg{i + 1}"));
                Emit(function.Start, 0, $"function {function.Name}({args})");
                Emit(-1, 0, "{");
            }

            static string TypeName(uint t) => t switch { 0 => "bool", 1 => "float", 2 => "string", _ => $"type{t}" };

            /// <summary>Inserts the labels for any address that ended up being the target of
            /// an emitted goto. Done last because whether a jump is structured or a goto is
            /// only known once the region containing it has been emitted.</summary>
            public List<Line> Finish()
            {
                foreach (var target in _labels.OrderByDescending(x => x))
                {
                    var at = _lines.FindIndex(l => l.Address >= target && l.Address != -1);
                    if (at < 0)
                        continue;
                    _lines.Insert(at, new Line(-1, Math.Max(0, _lines[at].Indent - 1), $"L{target}:"));
                }
                return _lines;
            }

            // ---- expression plumbing -------------------------------------------------

            /// <summary>Works out which slot each pending producer wrote to, by matching them
            /// against the operands of the consumer now being processed.
            ///
            /// Negative operands are frame slots - parameters and locals - which no producer
            /// ever writes, so they are never candidates. Of the rest, an operand can take a
            /// pending value when it is either free or stale, and is a plain re-read
            /// otherwise:
            ///
            /// FREE. Nothing has been bound to it yet, so it can only be a write.
            ///
            /// STALE. Something is bound, but from an earlier scope. Every slot is stamped
            /// with the epoch it was last written in and SCOPE_RESTORE - which opens the next
            /// arm - advances the epoch, so a slot number reused from a since-closed epoch is
            /// fair game for a fresh value. An epoch is the right granularity because it
            /// tracks what actually invalidates a slot's contents, unlike a numeric
            /// high-water mark: that always sits at the most recently written slot, which
            /// made every fresh write look simultaneously "reusable" to itself.
            ///
            /// LIVE AND CURRENT. Bound in this same arm, so the operand is reading it back
            /// rather than overwriting it - as in <c>CMP(a,"null"); CMP(a,""); OR(...)</c>,
            /// where the second comparison re-reads the slot holding <c>a</c> while writing
            /// only its own second operand, or <c>x = float(charging) / float(total)</c>,
            /// where the second conversion re-reads the total loaded two instructions
            /// earlier. Excluded, so its pending value stays for the consumer further down.
            ///
            /// Free operands are preferred over stale ones, but only up to the point where
            /// there are enough of them to receive every pending value. Both halves of that
            /// matter, and each corresponds to a case the other gets wrong:
            ///
            /// Preferring free is what protects a long-lived value. The campaign resolution
            /// chains load Feedback_Subculture_Player once before the chain and re-read that
            /// one slot in every arm; the epoch has advanced many times by then, so the slot
            /// looks stale even though the arm is only reading it. Treating free and stale as
            /// equally writable let its slot swallow a freshly loaded string, and the value
            /// that should have gone to the genuinely free operand went missing.
            ///
            /// Falling back to stale is what handles an operand pair of one of each. The
            /// pulse chains in AMM_pulses_choose_percussion compare <c>ams_pulse_type</c> - a
            /// slot the preceding arm left stale - against a freshly loaded <c>"both"</c>.
            /// Preferring free as a whole group left the stale operand never rebound, so it
            /// kept rendering the previous arm's comparison instead of the value just loaded
            /// into it.</summary>
            void Bind(params int[] slotOperands)
            {
                var candidates = slotOperands.Where(s => s >= 0).ToArray();
                var free = candidates.Where(s => !_slots.ContainsKey(s)).ToArray();

                var targets = free.Length >= _pending.Count
                    ? free
                    : candidates.Where(s => !_slots.ContainsKey(s) || _slotEpoch[s] < _epoch).ToArray();

                var n = Math.Min(_pending.Count, targets.Length);
                for (var i = 0; i < n; i++)
                {
                    var slot = targets[targets.Length - n + i];
                    _slots[slot] = _pending[_pending.Count - n + i];
                    _slotEpoch[slot] = _epoch;
                }
                _pending.RemoveRange(_pending.Count - n, n);
            }

            /// <summary>Binds a function-call argument. Unlike <see cref="Bind"/>, this
            /// doesn't assume the operand is the most recently pushed value: a call whose
            /// result will be used has the compiler push a placeholder for that result
            /// (typically <c>LOAD_INT 0</c>) immediately after the real argument values, so
            /// by the time the last argument is bound, the top of the pending list belongs
            /// to the placeholder, not the argument. Searching for a pending value of the
            /// expected type - string args are never satisfied by an int placeholder -
            /// finds the real argument regardless of position, and leaves the placeholder
            /// for <see cref="EmitCallOrExpr"/> to claim.
            ///
            /// "Already live" only counts if the slot's value dates from the current scope,
            /// for the same reason <see cref="Bind"/> checks the epoch rather than mere
            /// presence: a comparison two sibling branches back can leave an unrelated value
            /// sitting in this exact slot number (each branch of a chain like
            /// <c>arg2 == "good"</c>, <c>arg2 == "neutral"</c>, ... reuses the same scratch
            /// slot for its own comparison operand), and treating that as "still live" here
            /// re-passed a stale string literal from an already-exited comparison as a float
            /// argument, silently eating the real pending placeholder as an unrelated side
            /// effect further down.</summary>
            void BindArg(ExprKind expectedKind, int slot)
            {
                // A negative operand passes a parameter straight through (BIND_ARG_STRING -1
                // = "use this function's own arg1 as the argument") - nothing was pushed for
                // it, so there's nothing to claim from _pending. Get() resolves it directly.
                if (slot < 0)
                    return;
                if (_slots.ContainsKey(slot) && _slotEpoch[slot] >= _epoch)
                    return; // still live in this scope - this operand re-passes an existing value verbatim

                var index = _pending.FindLastIndex(p => p.Kind == expectedKind);
                if (index < 0)
                    index = _pending.Count - 1;
                if (index < 0)
                    return;

                _slots[slot] = _pending[index];
                _slotEpoch[slot] = _epoch;
                _pending.RemoveAt(index);
            }

            public bool IsComplete { get; private set; } = true;

            Expr Get(int slot)
            {
                if (slot < 0)
                    return new Expr(FrameName(slot), PrecAtom);
                if (_slots.TryGetValue(slot, out var e))
                    return e;

                IsComplete = false;
                return new Expr($"slot{slot}", PrecAtom);
            }

            /// <summary>Names a negative (frame-relative) slot: a declared parameter's own
            /// offset is that parameter, a declared result slot is that result, and anything
            /// else stays a raw frame reference rather than being guessed at. See
            /// <see cref="ParameterOffsets"/> for why this isn't a flat -1, -2, ... mapping
            /// and <see cref="ReturnOffsets"/> for where results sit.</summary>
            string FrameName(int slot)
            {
                var parameter = Array.IndexOf(_parameterOffsets, slot);
                if (parameter >= 0)
                    return $"arg{parameter + 1}";

                var result = Array.IndexOf(_returnOffsets, slot);
                return result >= 0 ? $"ret{result + 1}" : $"frame[{slot}]";
            }

            void Push(string text, int precedence, ExprKind kind = ExprKind.Unknown) =>
                _pending.Add(new Expr(text, precedence, kind));

            string Wrap(Expr e, int parentPrecedence) =>
                e.Precedence > parentPrecedence ? $"({e.Text})" : e.Text;

            void Binary(Instruction ins, string op, int precedence, ExprKind resultKind)
            {
                var a = Slot(ins, 0);
                var b = Slot(ins, 1);
                Bind(a, b);
                Push($"{Wrap(Get(a), precedence)} {op} {Wrap(Get(b), precedence)}", precedence, resultKind);
            }

            static int Slot(Instruction ins, int index) => unchecked((int)ins.Operands[index]);

            string Trigger(uint i) => i < file.Triggers.Count ? file.Triggers[(int)i].Name : $"trigger{i}";
            string Variable(uint i) => i < file.Variables.Count ? file.Variables[(int)i].Name : $"var{i}";
            string StringVariable(uint i) => i < file.StringVariables.Count ? file.StringVariables[(int)i].Name : $"strvar{i}";

            string Constant(uint i) =>
                i < file.StringConstants.Count ? Quote(file.StringConstants[(int)i]) : $"string{i}";

            static string Quote(string s) =>
                "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";

            // ---- statement / control flow --------------------------------------------

            public void EmitRange(int fromIndex, int toIndex, int indent)
            {
                var i = fromIndex;
                while (i < toIndex)
                {
                    var ins = _body[i];

                    if (ins.Opcode == 4 && TryEmitIf(ins, i, toIndex, indent, out var resumeIndex))
                    {
                        i = resumeIndex;
                        continue;
                    }

                    if (ins.Opcode == 55 && TryEmitSwitchCase(ins, i, toIndex, indent, out var switchResumeIndex))
                    {
                        i = switchResumeIndex;
                        continue;
                    }

                    EmitStatement(ins, indent);
                    i++;
                }
            }

            /// <summary>Structures a JUMP_IF_FALSE into if / if-else when both the body and any
            /// else region fall entirely inside the region being emitted. Returns false when
            /// they do not, so the caller falls back to emitting a conditional goto.</summary>
            bool TryEmitIf(Instruction ins, int index, int toIndex, int indent, out int resumeIndex)
            {
                resumeIndex = index;
                var target = (int)ins.Operands[0];
                if (!_indexOfAddress.TryGetValue(target, out var targetIndex) || targetIndex <= index || targetIndex > toIndex)
                    return false;

                var condSlot = Slot(ins, 1);
                Bind(condSlot);
                var condition = Get(condSlot).Text;

                // An if-body that ends by jumping forward over a following region is an else.
                var elseEndIndex = -1;
                var last = _body[targetIndex - 1];
                if (last.Opcode == 3 &&
                    _indexOfAddress.TryGetValue((int)last.Operands[0], out var afterElse) &&
                    afterElse > targetIndex && afterElse <= toIndex)
                    elseEndIndex = afterElse;

                var bodyEndIndex = elseEndIndex >= 0 ? targetIndex - 1 : targetIndex;

                // The compiler reuses the same scratch slots across sibling branches, since
                // at runtime only one of them ever executes. This decompiler isn't that
                // careful, so without isolating them explicitly, a value bound while
                // rendering the true branch would still be sitting in _slots when the false
                // branch is rendered next, and a slot number that means one thing in one
                // branch can easily mean something else entirely in the other (this is how
                // a stray value from one branch's assignment once turned up as a fabricated
                // condition in a completely unrelated sibling branch). Snapshotting right
                // after the condition and restoring it before each branch, and again before
                // whatever follows the if, keeps every branch reading only what was live at
                // the point they actually diverged.
                var snapshot = Snapshot();

                Emit(ins.Address, indent, $"if ({condition})");
                Emit(-1, indent, "{");
                EmitRange(index + 1, bodyEndIndex, indent + 1);
                Emit(-1, indent, "}");

                if (elseEndIndex >= 0)
                {
                    Restore(snapshot);
                    Emit(-1, indent, "else");
                    Emit(-1, indent, "{");
                    EmitRange(targetIndex, elseEndIndex, indent + 1);
                    Emit(-1, indent, "}");
                    resumeIndex = elseEndIndex;
                }
                else
                {
                    Restore(snapshot);
                    resumeIndex = targetIndex;
                }
                return true;
            }

            /// <summary>Structures a JUMP_IF_NOT_CASE the same way <see cref="TryEmitIf"/>
            /// structures a JUMP_IF_FALSE: the code between it and its target is that case's
            /// body, not unconditionally-skipped dead code. The instruction is one case of a
            /// PICK_ONE_OF chain - <c>JUMP_IF_NOT_CASE nextCase, valueSlot, caseN</c> falls
            /// through into the body when the selection in valueSlot is caseN, and jumps to
            /// the next case's test otherwise. Its body always ends in a return or a jump to
            /// a shared epilogue rather than reaching <c>nextCase</c> by falling through.
            ///
            /// Rendering it as a bare goto - which the decompiler used to do - made every
            /// case but the last look like unreachable code sitting after an unconditional
            /// jump (see debug_controls, where six real, differently-valued debug
            /// configurations all rendered as dead).
            ///
            /// The condition is a genuine one: valueSlot is bound by the PICK_ONE_OF that
            /// opens the chain (see EmitStatement case 54), so this reads back as
            /// <c>choiceN == 1</c>. Where that binding is somehow missing, Get() degrades to
            /// a bare slot name and marks the function incomplete rather than inventing
            /// anything.
            ///
            /// The case number is rendered one lower than the constant in the bytecode,
            /// which compares against 2..N+1 rather than 1..N. That <c>+1</c> is a fixed
            /// offset, not information: every chain in both shipped files starts at exactly
            /// 2 and runs contiguously (asserted by
            /// ChoicePointsTableDescribesEveryPickOneOfSite, so a build that ever broke the
            /// pattern would fail rather than silently renumber), so subtracting it is
            /// lossless and leaves a chain that reads consistently with the
            /// <c>PickOneOf(N)</c> above it - options 1..N for N options, instead of the
            /// 2..7-for-6-options the raw constants would otherwise show.
            ///
            /// What that offset means in the engine is not settled: the bytecode is equally
            /// consistent with a selection returning 1..N tested by "less than" thresholds
            /// (2..N+1 being the classic cumulative-threshold encoding of a uniform pick,
            /// where the last test can never fire and the last case is the catch-all) and
            /// with one returning 2..N+1 tested by equality. Both produce identical
            /// behaviour here - exactly one case runs - so the rendering commits only to
            /// which option was taken.</summary>
            bool TryEmitSwitchCase(Instruction ins, int index, int toIndex, int indent, out int resumeIndex)
            {
                resumeIndex = index;
                var target = (int)ins.Operands[0];
                if (!_indexOfAddress.TryGetValue(target, out var targetIndex) || targetIndex <= index || targetIndex > toIndex)
                    return false;

                var valueSlot = Slot(ins, 1);
                var optionNumber = unchecked((int)ins.Operands[2]) - 1;
                var snapshot = Snapshot();

                Emit(ins.Address, indent, $"if ({Get(valueSlot).Text} == {optionNumber})");
                Emit(-1, indent, "{");
                EmitRange(index + 1, targetIndex, indent + 1);
                Emit(-1, indent, "}");

                Restore(snapshot);
                resumeIndex = targetIndex;
                return true;
            }

            (Dictionary<int, Expr> Slots, Dictionary<int, int> SlotEpoch, List<Expr> Pending, int Epoch) Snapshot() =>
                (new Dictionary<int, Expr>(_slots), new Dictionary<int, int>(_slotEpoch), new List<Expr>(_pending), _epoch);

            void Restore((Dictionary<int, Expr> Slots, Dictionary<int, int> SlotEpoch, List<Expr> Pending, int Epoch) snapshot)
            {
                _slots.Clear();
                foreach (var (k, v) in snapshot.Slots) _slots[k] = v;
                _slotEpoch.Clear();
                foreach (var (k, v) in snapshot.SlotEpoch) _slotEpoch[k] = v;
                _pending.Clear();
                _pending.AddRange(snapshot.Pending);
                _epoch = snapshot.Epoch;
            }

            void EmitStatement(Instruction ins, int indent)
            {
                var o = ins.Operands;
                switch (ins.Opcode)
                {
                    case 0:
                        Emit(ins.Address, indent, "return;");
                        _pending.Clear(); // unreachable past here; a stray value must not leak into the next arm
                        return;

                    // Slot bookkeeping the source language has no equivalent for. Freed
                    // slots aren't removed from the binding map here - sibling branches are
                    // isolated at the point they diverge instead (see TryEmitIf), which is
                    // what freeing is really for from a decompiling point of view.
                    case 45: // FREE_SLOT
                        return;
                    case 6: // SCOPE_RESTORE - opens the next arm, so scratch slots are free again
                        _epoch++;
                        _pending.Clear(); // anything still pending here was never consumed by the arm that just ended
                        return;

                    // --- value producers ---
                    case 11: Push(Trigger(o[0]), PrecAtom, ExprKind.Numeric); return;
                    case 25: Push(Variable(o[0]), PrecAtom, ExprKind.Numeric); return;
                    case 41: Push(StringVariable(o[0]), PrecAtom, ExprKind.String); return;
                    case 46: Push(Constant(o[0]), PrecAtom, ExprKind.String); return;
                    case 15: Push(unchecked((int)o[0]).ToString(), PrecAtom, ExprKind.Numeric); return;
                    case 29: Push(Float(BitConverter.UInt32BitsToSingle(o[0])), PrecAtom, ExprKind.Numeric); return;

                    case 8: // NOT
                    {
                        var s = Slot(ins, 0);
                        Bind(s);
                        Push($"!{Wrap(Get(s), PrecUnary)}", PrecUnary, ExprKind.Numeric);
                        return;
                    }
                    case 19: // INT_TO_FLOAT
                    case 20: // INT_TO_STRING
                    case 33: // FLOAT_TO_INT (unseen - see Mnemonics)
                    case 34: // FLOAT_TO_STRING
                    case 51: // STRING_TO_INT
                    {
                        var s = Slot(ins, 0);
                        Bind(s);
                        var (conversion, kind) = ins.Opcode switch
                        {
                            19 => ("float", ExprKind.Numeric),
                            20 => ("string", ExprKind.String),
                            34 => ("string", ExprKind.String),
                            _ => ("int", ExprKind.Numeric),
                        };
                        Push($"{conversion}({Get(s).Text})", PrecAtom, kind);
                        return;
                    }

                    case 9: Binary(ins, "&&", PrecAnd, ExprKind.Numeric); return;
                    case 10: Binary(ins, "||", PrecOr, ExprKind.Numeric); return;
                    case 16: Binary(ins, "==", PrecCompare, ExprKind.Numeric); return;
                    case 47: Binary(ins, "==", PrecCompare, ExprKind.Numeric); return;
                    case 30: Binary(ins, "==", PrecCompare, ExprKind.Numeric); return; // CMP_EQ_FLOAT, unseen
                    case 17: Binary(ins, ">", PrecCompare, ExprKind.Numeric); return;
                    case 18: Binary(ins, ">=", PrecCompare, ExprKind.Numeric); return;
                    case 31: Binary(ins, ">", PrecCompare, ExprKind.Numeric); return;
                    case 32: Binary(ins, ">=", PrecCompare, ExprKind.Numeric); return;
                    case 21: Binary(ins, "+", PrecAdd, ExprKind.Numeric); return;
                    case 35: Binary(ins, "+", PrecAdd, ExprKind.Numeric); return;
                    case 53: Binary(ins, "+", PrecAdd, ExprKind.String); return;
                    case 22: Binary(ins, "-", PrecAdd, ExprKind.Numeric); return;
                    case 36: Binary(ins, "-", PrecAdd, ExprKind.Numeric); return; // SUB_FLOAT, unseen
                    case 23: Binary(ins, "*", PrecMul, ExprKind.Numeric); return; // MUL_INT, unseen
                    case 37: Binary(ins, "*", PrecMul, ExprKind.Numeric); return;
                    case 24: Binary(ins, "/", PrecMul, ExprKind.Numeric); return; // DIV_INT, unseen
                    case 38: Binary(ins, "/", PrecMul, ExprKind.Numeric); return;

                    // --- assignments ---
                    case 12: EmitAssign(ins, indent, Trigger(o[0]), Slot(ins, 1)); return;
                    case 26: EmitAssign(ins, indent, Variable(o[0]), Slot(ins, 1)); return;
                    case 42: EmitAssign(ins, indent, StringVariable(o[0]), Slot(ins, 1)); return;
                    case 14: EmitAssign(ins, indent, FrameName(unchecked((int)o[1])), Slot(ins, 0)); return;
                    case 28: EmitAssign(ins, indent, FrameName(unchecked((int)o[1])), Slot(ins, 0)); return; // STORE_SLOT_FLOAT
                    case 44: EmitAssign(ins, indent, FrameName(unchecked((int)o[1])), Slot(ins, 0)); return; // STORE_SLOT_STRING, unseen

                    // PICK_ONE_OF (optionCount, choiceId, inputSlot): the engine selects one of
                    // optionCount options and leaves it in slot inputSlot+4, which the
                    // JUMP_IF_NOT_CASE chain immediately following tests against each case
                    // number in turn. Binding that destination slot to a named value is what
                    // lets TryEmitSwitchCase render a real "choiceN == 2" condition instead of
                    // a placeholder. The input slot holds a literal whose meaning isn't
                    // established (see the Mnemonics comment); it's consumed so it can't leak
                    // into a later consumer, but isn't rendered as an argument.
                    case 54:
                    {
                        var inputSlot = Slot(ins, 2);
                        Bind(inputSlot);
                        var destSlot = inputSlot + 4;
                        var choiceName = $"choice{o[1]}";
                        _slots[destSlot] = new Expr(choiceName, PrecAtom, ExprKind.Numeric);
                        _slotEpoch[destSlot] = _epoch;
                        Emit(ins.Address, indent, $"{choiceName} = PickOneOf({unchecked((int)o[0])});");
                        return;
                    }

                    // --- calls ---
                    case 13: PushArg(ExprKind.Numeric, Slot(ins, 0)); return; // BIND_ARG_INT
                    case 27: PushArg(ExprKind.Numeric, Slot(ins, 0)); return; // BIND_ARG_FLOAT
                    case 43: PushArg(ExprKind.String, Slot(ins, 0)); return;  // BIND_ARG_STRING
                    case 1: EmitCallOrExpr(ins, indent, FunctionName((int)o[0])); return;
                    case 2: EmitCallOrExpr(ins, indent, IntrinsicName(o[0])); return;

                    // --- unstructured control flow ---
                    case 3:
                        EmitJump(ins, indent, (int)o[0], null);
                        _pending.Clear(); // unconditional - nothing pending here reaches the target
                        return;
                    case 4:
                    {
                        var s = Slot(ins, 1);
                        Bind(s);
                        EmitJump(ins, indent, (int)o[0], $"!({Get(s).Text})");
                        return;
                    }
                    case 55:
                        // Fallback for a JUMP_IF_NOT_CASE TryEmitSwitchCase couldn't structure
                        // (target outside the region being emitted, or pointing backward) -
                        // rendered as a conditional goto rather than guessed at, same as any
                        // other jump that doesn't fit a structured shape.
                        Emit(ins.Address, indent,
                            $"if ({Get(Slot(ins, 1)).Text} != {unchecked((int)o[2]) - 1}) goto L{(int)o[0]};");
                        _labels.Add((int)o[0]);
                        return;

                    default:
                        // An opcode we don't understand might consume the currently pending
                        // value, produce a new one, both, or neither - since we don't know,
                        // any pending value is discarded rather than left for a later
                        // consumer to inherit as if this opcode were a no-op (which is how
                        // MM_update_from_music_marker's op51/op39 pair, whatever they
                        // actually do to the loaded string, previously let a completely
                        // unrelated STORE_TRIGGER downstream silently claim the string
                        // itself). The function is marked incomplete either way.
                        //
                        // The one thing kept is a run of placeholders from immediately
                        // preceding unknown opcodes with nothing pushed in between - back to
                        // back unknown opcodes (op51 directly followed by op39, say) are
                        // themselves evidence each one is a real step of the same
                        // computation, and clearing on the second one destroyed the first
                        // one's placeholder before anything downstream could bind to it,
                        // which is what turned "op51's result" into a bare, unexplained
                        // slotN in the rendering. A placeholder is never mistaken for a real
                        // stale value here since nothing but this default case ever produces
                        // the "/* opN result */" text.
                        IsComplete = false;
                        var keep = 0;
                        while (keep < _pending.Count &&
                               _pending[_pending.Count - 1 - keep].Text.StartsWith("/* op", StringComparison.Ordinal))
                            keep++;
                        if (keep < _pending.Count)
                            _pending.RemoveRange(0, _pending.Count - keep);
                        Push($"/* op{ins.Opcode} result */", PrecAtom);
                        Emit(ins.Address, indent,
                            $"op{ins.Opcode}({string.Join(", ", o.Select(x => unchecked((int)x)))});");
                        return;
                }
            }

            void EmitAssign(Instruction ins, int indent, string target, int sourceSlot)
            {
                Bind(sourceSlot);
                Emit(ins.Address, indent, $"{target} = {Get(sourceSlot).Text};");
            }

            void PushArg(ExprKind kind, int slot)
            {
                BindArg(kind, slot);
                _callArgs.Add(Get(slot).Text);
            }

            /// <summary>A call whose result is used has the compiler push a placeholder for
            /// that result right after its arguments (see <see cref="BindArg"/>) - by the
            /// time CALL itself executes, that placeholder is the most recently pushed
            /// pending value. Claiming it and re-pushing the call as an expression, instead
            /// of also emitting it as its own statement, is what turns
            /// <c>Foo(); if (slotN)</c> into the correct <c>if (Foo())</c>: the call has a
            /// single textual occurrence either way, this just picks the right one.
            ///
            /// A 0-argument call preceded by exactly one placeholder is that single-result
            /// case. One preceded by two or more placeholders is a different, equally real
            /// convention: the callee has no BIND_ARG-consumed parameters at all and
            /// communicates purely by writing into its own leading frame slots, which alias
            /// the caller's placeholder cells (see battle's update_Check_Battle_End_Status,
            /// which unconditionally assigns <c>frame[-4]</c> and <c>frame[-8]</c> on every
            /// return path, and is called with exactly two zeroed placeholders immediately
            /// ahead of it - the same shape
            /// as every other 0-arg call preceded by 2+ placeholders in this corpus, not a
            /// one-off). There is no single "the return value" to fold into an expression
            /// there, so the call stays its own statement and each placeholder is renamed to
            /// identify which of the callee's outputs a later consumer reads - the
            /// alternative, leaving it as a bare literal <c>0</c>, is indistinguishable from
            /// genuine hardcoded-disabled code and was exactly how this looked before this
            /// pattern was identified (<c>if (update_Check_Battle_End_Status())</c> followed by a
            /// sibling <c>if (0)</c> that was really reading the same call's second output).
            ///
            /// Only a bare <c>0</c> is recognised as a placeholder at all - every confirmed
            /// instance of this pattern is a literal zero - and only the trailing run of
            /// them, not every pending value: a function can also leave an unrelated,
            /// non-zero value pending across a call it doesn't use (its declared arg count
            /// can exceed its local BIND_ARGs, so some arguments are evidently supplied some
            /// other way), and claiming that would fabricate a result out of an unrelated
            /// variable.</summary>
            void EmitCallOrExpr(Instruction ins, int indent, string name)
            {
                var argsText = string.Join(", ", _callArgs);
                _callArgs.Clear();

                var zeroRun = 0;
                while (zeroRun < _pending.Count &&
                       _pending[_pending.Count - 1 - zeroRun] is { Kind: ExprKind.Numeric, Text: "0" or "0.0" })
                    zeroRun++;

                if (zeroRun == 1)
                {
                    _pending.RemoveAt(_pending.Count - 1);
                    Push($"{name}({argsText})", PrecAtom);
                    return;
                }

                if (zeroRun >= 2)
                {
                    for (var k = 0; k < zeroRun; k++)
                    {
                        var i = _pending.Count - zeroRun + k;
                        _pending[i] = _pending[i] with { Text = $"{name}_out{k + 1}" };
                    }
                }

                Emit(ins.Address, indent, $"{name}({argsText});");
            }

            /// <summary>A jump into a run of pure slot bookkeeping that ends in RETURN is the
            /// shared epilogue an else-if chain branches to, so it is rendered as a return.
            /// Everything else stays an explicit goto.</summary>
            void EmitJump(Instruction ins, int indent, int target, string? condition)
            {
                string text;
                if (JumpsToEpilogue(target))
                {
                    text = "return;";
                }
                else
                {
                    text = $"goto L{target};";
                    _labels.Add(target);
                }
                Emit(ins.Address, indent, condition == null ? text : $"if ({condition}) {text}");
            }

            bool JumpsToEpilogue(int target)
            {
                if (!_indexOfAddress.TryGetValue(target, out var i))
                    return false;
                for (; i < _body.Count; i++)
                {
                    if (_body[i].Opcode == 0)
                        return true;
                    if (_body[i].Opcode is not (45 or 6))
                        return false;
                }
                return false;
            }

            string FunctionName(int codeOffset) =>
                file.Functions.FirstOrDefault(f => f.CodeOffset == codeOffset)?.Name ?? $"function_at_{codeOffset}";

            string IntrinsicName(uint index) =>
                index < file.Intrinsics.Count
                    ? file.Intrinsics[(int)index].Name.Replace(" ", "")
                    : $"intrinsic{index}";

            static string Float(float f) => f == (int)f ? $"{(int)f}.0" : f.ToString("0.######");
        }
    }
}
