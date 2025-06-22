namespace DynDungeonCrawler.Engine.Interfaces
{
    public interface IUserInterface
    {
        void Write(string message);

        void WriteLine(string message);

        string ReadLine();

        ConsoleKey ReadKey(bool intercept = false);

        void Clear();
    }
}