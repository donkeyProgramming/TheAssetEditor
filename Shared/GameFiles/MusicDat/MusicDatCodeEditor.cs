namespace Shared.GameFormats.MusicDat
{
    /// <summary>
    /// The one structural edit the music script needs: splicing new words into the shared
    /// instruction stream and repairing everything that referred to an address past the
    /// splice point.
    ///
    /// Three things in the file hold code addresses, and all three are relocated here:
    /// the function table's CodeOffset, the CALL opcode's target, and the jump opcodes'
    /// targets. Nothing else does - the symbol tables are referenced by index, so growing
    /// them never disturbs existing code, and ChoicePoints holds an id and an option count
    /// rather than any offset. ChoicePoints is still the one table an insertion can break,
    /// because its ids are positional; see <see cref="Validate"/>.
    ///
    /// Addresses strictly greater than the splice point move; an address equal to it does
    /// not, so a jump that pointed at <paramref name="atAddress"/> ends up pointing at the
    /// newly inserted code. That is what makes "insert a new arm in front of an existing
    /// one" work: the preceding arm's JUMP_IF_FALSE keeps its value and now selects the new
    /// arm, whose own exit jump is authored to point at the old one.
    ///
    /// <see cref="Validate"/> re-derives the whole program afterwards and is the guard that
    /// a caller should refuse to save without.
    /// </summary>
    public static class MusicDatCodeEditor
    {
        // Opcodes whose first operand is an address into the instruction stream.
        static readonly HashSet<uint> JumpOpcodes = [3, 4, 55];
        const uint CallOpcode = 1;
        const uint PickOneOfOpcode = 54;

        /// <summary>Appends a string to the constant pool, reusing an existing entry when the
        /// value is already present. Safe at any time: constants are referenced by index.</summary>
        public static int AddStringConstant(MusicDatFile file, string value)
        {
            var existing = file.StringConstants.IndexOf(value);
            if (existing >= 0)
                return existing;

            file.StringConstants.Add(value);
            return file.StringConstants.Count - 1;
        }

        /// <summary>Splices <paramref name="words"/> into the instruction stream at
        /// <paramref name="atAddress"/> and relocates every address that referred past it.
        /// The inserted words are used verbatim - the caller is responsible for any addresses
        /// inside them (see <see cref="MusicDatCultureWiring"/> for how a cloned block does
        /// that). The new code belongs to whichever function contains
        /// <paramref name="atAddress"/>.</summary>
        public static void InsertCode(MusicDatFile file, int atAddress, IReadOnlyList<uint> words)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(atAddress);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(atAddress, file.Instructions.Count);
            if (words.Count == 0)
                return;

            var functions = MusicDatDisassembler.Disassemble(file);
            var undecodable = functions.Where(f => !f.DecodedExactly).Select(f => f.Name).ToList();
            if (undecodable.Count > 0)
                throw new InvalidOperationException(
                    $"Refusing to edit code: these functions do not disassemble exactly, so their instruction " +
                    $"boundaries are unknown and relocation would corrupt them: {string.Join(", ", undecodable)}");

            var all = functions.SelectMany(f => f.Instructions).ToList();
            if (atAddress != file.Instructions.Count && all.All(i => i.Address != atAddress))
                throw new ArgumentException($"Address {atAddress} is not an instruction boundary.", nameof(atAddress));

            // Collected against the pre-insert stream, where opcode positions are known.
            var addressWordIndices = all
                .Where(i => JumpOpcodes.Contains(i.Opcode) || i.Opcode == CallOpcode)
                .Select(i => i.Address + 1)
                .ToList();

            var shift = words.Count;
            file.Instructions.InsertRange(atAddress, words);

            foreach (var oldIndex in addressWordIndices)
            {
                var index = oldIndex >= atAddress ? oldIndex + shift : oldIndex;
                var target = file.Instructions[index];
                if (target > atAddress)
                    file.Instructions[index] = target + (uint)shift;
            }

            foreach (var function in file.Functions)
                if (function.CodeOffset > atAddress)
                    function.CodeOffset += (uint)shift;
        }

        /// <summary>Re-derives the program and reports every way it is now inconsistent.
        /// An empty list means the file decodes exactly as it did before, with all control
        /// flow and every table reference in range.</summary>
        public static IReadOnlyList<string> Validate(MusicDatFile file)
        {
            var problems = new List<string>();
            var functions = MusicDatDisassembler.Disassemble(file);
            var functionStarts = file.Functions.Select(f => (int)f.CodeOffset).ToHashSet();

            foreach (var function in functions)
            {
                if (!function.DecodedExactly)
                {
                    problems.Add($"{function.Name}: does not disassemble into whole instructions.");
                    continue;
                }

                var boundaries = function.Instructions.Select(i => i.Address).ToHashSet();
                boundaries.Add(function.End);

                foreach (var ins in function.Instructions)
                {
                    if (JumpOpcodes.Contains(ins.Opcode) && !boundaries.Contains((int)ins.Operands[0]))
                        problems.Add($"{function.Name}@{ins.Address}: jump to {ins.Operands[0]} is not an instruction boundary in this function.");

                    if (ins.Opcode == CallOpcode && !functionStarts.Contains((int)ins.Operands[0]))
                        problems.Add($"{function.Name}@{ins.Address}: call to {ins.Operands[0]} is not the start of any function.");

                    var problem = CheckTableReference(file, ins);
                    if (problem != null)
                        problems.Add($"{function.Name}@{ins.Address}: {problem}");
                }
            }

            problems.AddRange(ValidateChoicePoints(functions, file));
            return problems;
        }

        /// <summary>Checks the one table that insertion cannot fix up on its own.
        ///
        /// A PICK_ONE_OF names its entry in <see cref="MusicDatFile.ChoicePoints"/> by an id
        /// that is also that entry's index, assigned in bytecode order - so unlike every other
        /// table here, which is referenced by a stable index, this one is positional. Splicing
        /// in code that contains a PICK_ONE_OF therefore needs a matching entry inserted and
        /// every later id renumbered, which <see cref="InsertCode"/> deliberately does not
        /// attempt. Cloning one would also duplicate its id, quietly pointing two selections at
        /// one entry.
        ///
        /// None of the edits this class currently supports move a PICK_ONE_OF - the culture
        /// chains contain none - so this is a guard against a future edit that does, rather
        /// than a live failure. It is checked here because Validate is what callers gate on.</summary>
        static IEnumerable<string> ValidateChoicePoints(
            IReadOnlyList<MusicDatDisassembler.Function> functions, MusicDatFile file)
        {
            var sites = functions
                .SelectMany(f => f.Instructions.Select(i => (Function: f, Instruction: i)))
                .Where(x => x.Instruction.Opcode == PickOneOfOpcode)
                .OrderBy(x => x.Instruction.Address)
                .ToList();

            if (sites.Count != file.ChoicePoints.Count)
            {
                yield return $"choice point table has {file.ChoicePoints.Count} entries but the code contains " +
                             $"{sites.Count} PICK_ONE_OF instructions.";
                yield break;
            }

            for (var i = 0; i < sites.Count; i++)
            {
                var (function, ins) = sites[i];
                var choiceId = (int)ins.Operands[1];

                if (choiceId != i)
                {
                    yield return $"{function.Name}@{ins.Address}: PICK_ONE_OF names choice point {choiceId}, but it is " +
                                 $"the {i} th in bytecode order and the id has to match that position.";
                    continue;
                }

                if (file.ChoicePoints[choiceId].OptionCount != ins.Operands[0])
                    yield return $"{function.Name}@{ins.Address}: PICK_ONE_OF selects between {ins.Operands[0]} options " +
                                 $"but choice point {choiceId} declares {file.ChoicePoints[choiceId].OptionCount}.";
            }
        }

        static string? CheckTableReference(MusicDatFile file, MusicDatDisassembler.Instruction ins)
        {
            var index = ins.Operands.Length > 0 ? ins.Operands[0] : 0;
            return ins.Opcode switch
            {
                11 or 12 when index >= file.Triggers.Count => $"trigger index {index} out of range.",
                25 or 26 when index >= file.Variables.Count => $"variable index {index} out of range.",
                41 or 42 when index >= file.StringVariables.Count => $"string variable index {index} out of range.",
                46 when index >= file.StringConstants.Count => $"string constant index {index} out of range.",
                2 when index >= file.Intrinsics.Count => $"intrinsic index {index} out of range.",
                _ => null
            };
        }
    }
}
