namespace DynDungeonCrawler.Engine.Interfaces
{
    public interface IUserInterface
    {
        void Write(string message);

        void WriteLine(string message);

        void WriteLine();

        string ReadLine();

        ConsoleKey ReadKey(bool intercept = false);

        void Clear();
    }
}