
namespace TWWHeapVisualizer.Dolphin
{
    public class ClassAttribute : Attribute
    {
        public string name { get; }

        public ClassAttribute(string name)
        {
            this.name = name;
        }
    }
}