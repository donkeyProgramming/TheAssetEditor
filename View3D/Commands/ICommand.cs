namespace View3D.Commands
{
    public interface ICommand
    {
        void Undo();
        void Execute();
    }
}

