public abstract class Animal
{
    public string name;
    public int age;

    public Animal(int age, string name)
    {
        this.age = age;
        this.name = name;
    }





}

public interface IVoicable
{
    public void Voice();
}
