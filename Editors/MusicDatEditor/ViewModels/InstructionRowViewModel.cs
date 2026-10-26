using Shared.GameFormats.MusicDat;

namespace Editors.MusicDatEditor.ViewModels
{
    /// <summary>One instruction row in the decompiled view - a read-only rendering of one word
    /// group in <see cref="MusicDatFile.Instructions"/>.</summary>
    public class InstructionRowViewModel
    {
        public int Address { get; }
        public uint Opcode { get; }
        public string Mnemonic { get; }
        public int OperandCount { get; }

        public bool HasOperand0 => OperandCount > 0;
        public bool HasOperand1 => OperandCount > 1;
        public bool HasOperand2 => OperandCount > 2;

        public string Operand0Text { get; }
        public string Operand1Text { get; }
        public string Operand2Text { get; }
        public string Annotation { get; }

        public InstructionRowViewModel(MusicDatDisassembler.Instruction instruction, MusicDatFile file,
            IReadOnlyDictionary<int, string> functionNamesByOffset)
        {
            Address = instruction.Address;
            Opcode = instruction.Opcode;
            Mnemonic = instruction.Mnemonic;
            OperandCount = instruction.Operands.Length;

            uint GetOperand(int index) => file.Instructions[Address + 1 + index];

            string FormatOperand(int index)
            {
                if (index >= OperandCount)
                    return "";

                var value = GetOperand(index);
                if (index == 0 && Opcode == 29) // LOAD_FLOAT: raw bits -> readable float
                    return BitConverter.UInt32BitsToSingle(value).ToString("0.######");
                if (index == 0 && Opcode == 15) // LOAD_INT: display signed
                    return unchecked((int)value).ToString();
                return value.ToString();
            }

            Operand0Text = FormatOperand(0);
            Operand1Text = FormatOperand(1);
            Operand2Text = FormatOperand(2);
            Annotation = BuildAnnotation(file, functionNamesByOffset, GetOperand);
        }

        string BuildAnnotation(MusicDatFile file, IReadOnlyDictionary<int, string> functionNamesByOffset, Func<int, uint> getOperand)
        {
            if (OperandCount == 0)
                return "";

            var o0 = getOperand(0);
            switch (Opcode)
            {
                case 11 or 12 when o0 < file.Triggers.Count:
                    return $"trigger: {file.Triggers[(int)o0].Name}";
                case 25 or 26 when o0 < file.Variables.Count:
                    return $"var: {file.Variables[(int)o0].Name}";
                case 41 or 42 when o0 < file.StringVariables.Count:
                    return $"strvar: {file.StringVariables[(int)o0].Name}";
                case 46 when o0 < file.StringConstants.Count:
                    return $"\"{Trim(file.StringConstants[(int)o0])}\"";
                case 29:
                    return $"float {BitConverter.UInt32BitsToSingle(o0)}";
                case 1:
                    return $"-> {(functionNamesByOffset.TryGetValue((int)o0, out var n) ? n : "?")}  args={(OperandCount > 1 ? getOperand(1) : 0)}";
                case 2 when o0 < file.Intrinsics.Count:
                    return $"call: {file.Intrinsics[(int)o0].Name}";
                default:
                    return "";
            }
        }

        static string Trim(string s)
        {
            s = s.Replace("\r", "\\r").Replace("\n", "\\n");
            return s.Length > 60 ? s[..60] + "..." : s;
        }
    }
}
