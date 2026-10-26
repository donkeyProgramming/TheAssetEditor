using System.Text;

namespace Shared.GameFormats.MusicDat
{
    /// <summary>
    /// Disassembler for the bytecode stream in campaign_music.dat / battle_music.dat.
    ///
    /// The instruction stream (<see cref="MusicDatFile.Instructions"/>) is a flat uint32
    /// array of (opcode, operand...) instructions. Each entry in the function table
    /// (<see cref="MusicDatFile.Functions"/>) points at a start offset via CodeOffset; a
    /// function runs until the next function's start offset (or end of the array).
    /// </summary>
    public static class MusicDatDisassembler
    {
        // opcode -> number of operand words following it. Recovered by constraint-solving
        // both shipped files together: every function must tile exactly, table/call/jump
        // operands must land in range, and the result is unique.
        public static readonly IReadOnlyDictionary<uint, int> OperandCount = new Dictionary<uint, int>
        {
            [0] = 0,  [1] = 2,  [2] = 1,  [3] = 1,  [4] = 2,  [6] = 1,  [8] = 1,
            [9] = 2,  [10] = 2, [11] = 1, [12] = 2, [13] = 1, [14] = 2, [15] = 1,
            [16] = 2, [17] = 2, [18] = 2, [19] = 1, [20] = 1, [21] = 2, [22] = 2,
            [23] = 2, [24] = 2,
            [25] = 1, [26] = 2, [27] = 1, [28] = 2, [29] = 1,
            [30] = 2, [31] = 2, [32] = 2, [33] = 1, [34] = 1, [35] = 2, [36] = 2, [37] = 2, [38] = 2,
            [41] = 1, [42] = 2, [43] = 1, [44] = 2, [45] = 1, [46] = 1, [47] = 2, [51] = 1,
            [53] = 2, [54] = 3, [55] = 3,
        };
        // 23/24/30/33/36/44 aren't in either shipped file - see the Mnemonics comments
        // below for why they're pencilled in anyway. Wrong guesses fail silently rather
        // than loudly (see the 34/51 story below), so treat these six as more provisional
        // than the rest of the table if a future build ever exercises one of them.

        // Separately: an earlier solve had 34 and 51 taking no operands, so their operand
        // word got misread as an opcode of its own, producing phantom "opcodes" 24, 30 and
        // 39 that appeared nowhere except right after a 34 or a 51. Giving 34/51 their
        // operand back removed all three and made the surrounding code decode sensibly.
        // That's unrelated to
        // 24 and 30 being pencilled in above - those are separate, later guesses that the
        // numbers simply happen to share.

        // The VM is a slot machine: value-producing instructions write to an implicit temp
        // slot, consumers reference it by index. Type-specialised - separate int/float/
        // string variants of compare, arithmetic and argument binding - laid out below in
        // per-type blocks (int 13-24, float 25-38, string 41-53) that each run LOAD,
        // compares, conversions, then arithmetic in the same order.
        static readonly Dictionary<uint, string> Mnemonics = new()
        {
            [0] = "RETURN",             // ends the function, not just a block
            [1] = "CALL",               // (functionStartOffset, argCount)
            [2] = "CALL_INTRINSIC",     // (intrinsicIndex)
            [3] = "JUMP",               // (target)
            [4] = "JUMP_IF_FALSE",      // (target, conditionSlot)
            // 5: unseen, weak guess - JUMP_IF_TRUE, a natural counterpart to JUMP_IF_FALSE
            //    that a compiler emitting only if/else-if chains would never need.
            [6] = "SCOPE_RESTORE",      // (depth) - precedes JUMP / else-branches
            // 7: unseen, weak guess - a paired SCOPE_SAVE. No real evidence either way.
            [8] = "NOT",                // (slot)
            [9] = "AND",                // (slotA, slotB)
            [10] = "OR",                // (slotA, slotB)
            [11] = "LOAD_TRIGGER",      // (triggerIndex)
            [12] = "STORE_TRIGGER",     // (triggerIndex, sourceSlot)
            [13] = "BIND_ARG_INT",      // (slot)
            [14] = "STORE_SLOT",        // (sourceSlot, frameOffset)
            [15] = "LOAD_INT",          // (value)
            [16] = "CMP_EQ_INT",        // (slotA, slotB)
            [17] = "CMP_GT_INT",        // (slotA, slotB)
            [18] = "CMP_GE_INT",        // (slotA, slotB)
            [19] = "INT_TO_FLOAT",      // (slot)
            [20] = "INT_TO_STRING",     // (slot)
            [21] = "ADD_INT",           // (slotA, slotB)
            [22] = "SUB_INT",           // (slotA, slotB)
            // Int's block runs LOAD/EQ/GT/GE/TO_FLOAT/TO_STRING/ADD/SUB at a constant +14
            // offset from float's - every known opcode on both sides lines up under that
            // offset. 23/24 unseen, but that offset is exactly why they're filled in:
            [23] = "MUL_INT",           // (slotA, slotB) - unseen; +14 = MUL_FLOAT(37)
            [24] = "DIV_INT",           // (slotA, slotB) - unseen; +14 = DIV_FLOAT(38)
            [25] = "LOAD_VAR",          // (variableIndex)
            [26] = "STORE_VAR",         // (variableIndex, sourceSlot)
            [27] = "BIND_ARG_FLOAT",    // (slot)
            [28] = "STORE_SLOT_FLOAT",  // (sourceSlot, frameOffset) - "FLOAT" half inferred,
                // not solved: every site follows a Set RTPC call and precedes RETURN, matching
                // STORE_SLOT's own return convention, but STORE_SLOT is itself type-agnostic.
            [29] = "LOAD_FLOAT",        // (float bits)
            [30] = "CMP_EQ_FLOAT",      // (slotA, slotB) - unseen; +14 = CMP_EQ_INT(16).
                // Never compared for exact equality - GT/GE suit continuous values better.
            [31] = "CMP_GT_FLOAT",      // (slotA, slotB)
            [32] = "CMP_GE_FLOAT",      // (slotA, slotB)
            [33] = "FLOAT_TO_INT",      // (slot) - unseen; +14 = INT_TO_FLOAT(19).
                // Never truncated back to an int here.
            [34] = "FLOAT_TO_STRING",   // (slot) - the return trip INT_TO_STRING(20) implies
            [35] = "ADD_FLOAT",         // (slotA, slotB)
            [36] = "SUB_FLOAT",         // (slotA, slotB) - unseen; +14 = SUB_INT(22).
                // Never subtracted one float from another here.
            [37] = "MUL_FLOAT",         // (slotA, slotB)
            [38] = "DIV_FLOAT",         // (slotA, slotB)
            // 39/40: unseen, no candidate.
            [41] = "LOAD_STRVAR",       // (stringVariableIndex)
            [42] = "STORE_STRVAR",      // (stringVariableIndex, sourceSlot)
            [43] = "BIND_ARG_STRING",   // (slot)
            [44] = "STORE_SLOT_STRING", // (sourceSlot, frameOffset) - unseen, but STORE_SLOT(14)
                // and STORE_SLOT_FLOAT(28) both sit right after that type's BIND_ARG_*, and 44
                // is that same position after BIND_ARG_STRING(43). No function here returns a
                // bare string - string results go out through STORE_STRVAR instead.
            [45] = "FREE_SLOT",         // (slot)
            [46] = "LOAD_STRING",       // (stringIndex)
            [47] = "CMP_EQ_STRING",     // (slotA, slotB)
            // 48/49: unseen, weaker guess - CMP_GT_STRING/CMP_GE_STRING by analogy with int's
            //    three-way EQ/GT/GE, but string has no real use for lexicographic ordering.
            // 50: unseen, no fixed candidate - possibly STRING_TO_FLOAT (see 51/52 below).
            [51] = "STRING_TO_INT",     // (slot) - mirror of FLOAT_TO_STRING(34)
            // 52: unseen, no fixed candidate either - a STRING_TO_FLOAT plausibly sits at 50
            //    or 52 (adjacent to STRING_TO_INT the way INT_TO_FLOAT/INT_TO_STRING are),
            //    but nothing pins down which, so neither is filled in.
            [53] = "CONCAT_STRING",     // (slotA, slotB)
            [54] = "PICK_ONE_OF",       // (optionCount, choiceId, inputSlot) - the engine
                // picks one of optionCount options for ChoicePoints[choiceId] and leaves the
                // selection in inputSlot+4 for the JUMP_IF_NOT_CASE chain that always follows.
                // inputSlot always holds a literal, never a trigger/variable, so nothing in the
                // script itself makes the choice.
            [55] = "JUMP_IF_NOT_CASE",  // (target, valueSlot, caseNumber) - per-case test of a
                // PICK_ONE_OF chain; falls through when valueSlot == caseNumber, else jumps to
                // the next case. Cases in a chain of N are numbered 2..N+1.
            // 55 is just the highest opcode either file happens to use, not a confirmed
            // ceiling - nothing here rules out the ISA continuing past it (56, ...).
        };

        public record Instruction(int Address, uint Opcode, uint[] Operands)
        {
            public string Mnemonic => Mnemonics.TryGetValue(Opcode, out var m) ? m : $"op{Opcode}";
        }

        public record Function(string Name, int Start, int End, List<Instruction> Instructions, bool DecodedExactly, bool IsCalledInternally, List<uint> ParameterTypes)
        {
            /// <summary>True for functions that are (a) never reached by a CALL from any other
            /// function in this file, and (b) share a name with the small set of functions that
            /// show this same "never called" pattern in BOTH campaign_music.dat and
            /// battle_music.dat independently - strong evidence they're the game engine's actual
            /// entry points into the script, not just dead/legacy code. See
            /// <see cref="KnownEntryPointNames"/>.</summary>
            public bool IsKnownEntryPoint => !IsCalledInternally && KnownEntryPointNames.Contains(Name);

            /// <summary>Never called internally, but not one of the cross-file-confirmed entry
            /// point names above - likely dead/legacy code (several such functions exist per
            /// file, each with a different name, so they don't look like a shared engine
            /// convention the way the entry points do), but flagged rather than asserted.</summary>
            public bool IsLikelyUnreachable => !IsCalledInternally && !IsKnownEntryPoint;
        }

        /// <summary>Confirmed by cross-referencing both shipped Warhammer 3 files: these are the
        /// only function names that are (1) never CALLed by anything else in their own file, in
        /// (2) both files independently. Everything else reachable in the script is only ever
        /// reached via an internal CALL, so these are almost certainly the engine's own hooks -
        /// Initialise (script load), Update (per-tick), Music Marker (Wwise music sync callback),
        /// On Showing Results (post-battle/turn results screen, matches its own logic: iterates
        /// every playable faction posting a "winning faction" announcement event).</summary>
        public static readonly IReadOnlySet<string> KnownEntryPointNames =
            new HashSet<string> { "Initialise", "Update", "Music Marker", "On Showing Results" };

        public static List<Function> Disassemble(MusicDatFile file)
        {
            var result = new List<Function>();
            var sorted = file.Functions.OrderBy(x => x.CodeOffset).ToList();
            var calledOffsets = new HashSet<int>();

            for (var i = 0; i < sorted.Count; i++)
            {
                var start = (int)sorted[i].CodeOffset;
                var end = i + 1 < sorted.Count ? (int)sorted[i + 1].CodeOffset : file.Instructions.Count;

                var body = new List<Instruction>();
                var p = start;
                var exact = true;
                while (p < end)
                {
                    var op = file.Instructions[p];
                    if (!OperandCount.TryGetValue(op, out var n) || p + 1 + n > end)
                    {
                        exact = false;
                        break;
                    }
                    var operands = new uint[n];
                    for (var k = 0; k < n; k++)
                        operands[k] = file.Instructions[p + 1 + k];
                    body.Add(new Instruction(p, op, operands));
                    if (op == 1) // CALL(functionStartOffset, argCount)
                        calledOffsets.Add((int)operands[0]);
                    p += 1 + n;
                }
                if (p != end) exact = false;

                result.Add(new Function(sorted[i].Name, start, end, body, exact, IsCalledInternally: false, sorted[i].ParameterTypes));
            }

            for (var i = 0; i < result.Count; i++)
                if (calledOffsets.Contains(result[i].Start))
                    result[i] = result[i] with { IsCalledInternally = true };

            return result;
        }

        public static string ToText(MusicDatFile file)
        {
            var sb = new StringBuilder();
            var funcs = Disassemble(file);
            var byOffset = funcs.ToDictionary(f => f.Start, f => f.Name);

            sb.AppendLine($"functions decoding exactly: {funcs.Count(f => f.DecodedExactly)}/{funcs.Count}");

            foreach (var f in funcs)
            {
                sb.AppendLine($"\n== {f.Name}  [{f.Start}..{f.End})  len={f.End - f.Start}{(f.DecodedExactly ? "" : "   *** PARTIAL DECODE ***")} ==");
                foreach (var ins in f.Instructions)
                {
                    var ops = string.Join(", ", ins.Operands.Select(o => unchecked((int)o).ToString()));
                    sb.AppendLine($"  [{ins.Address,6}] {ins.Mnemonic,-16} {ops,-22}{Annotate(file, ins, byOffset)}");
                }
            }
            return sb.ToString();
        }

        static string Annotate(MusicDatFile file, Instruction ins, Dictionary<int, string> byOffset)
        {
            var o = ins.Operands;
            switch (ins.Opcode)
            {
                case 11 or 12 when o[0] < file.Triggers.Count:
                    return $"  ; {file.Triggers[(int)o[0]].Name}";
                case 25 or 26 when o[0] < file.Variables.Count:
                    return $"  ; {file.Variables[(int)o[0]].Name}";
                case 41 or 42 when o[0] < file.StringVariables.Count:
                    return $"  ; strvar {file.StringVariables[(int)o[0]].Name}";
                case 46 when o[0] < file.StringConstants.Count:
                    return $"  ; \"{Trim(file.StringConstants[(int)o[0]])}\"";
                case 29:
                    return $"  ; float {BitConverter.UInt32BitsToSingle(o[0])}";
                case 1:
                    return $"  ; -> {(byOffset.TryGetValue((int)o[0], out var n) ? n : "?")}  args={o[1]}";
                case 2 when o[0] < file.Intrinsics.Count:
                    return $"  ; {file.Intrinsics[(int)o[0]].Name}";
                default:
                    return "";
            }
        }

        static string Trim(string s)
        {
            s = s.Replace("\r", "\\r").Replace("\n", "\\n");
            return s.Length > 70 ? s[..70] + "..." : s;
        }
    }
}
