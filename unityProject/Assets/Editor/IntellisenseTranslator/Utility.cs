public class Utility
{
    private static string _delimitar = System.IO.Path.DirectorySeparatorChar.ToString();

    public static string ConvertPath(string path)
    {
        path = path.Replace("/", _delimitar);
        path = path.Replace(@"\", _delimitar);

        return path;
    }
}
