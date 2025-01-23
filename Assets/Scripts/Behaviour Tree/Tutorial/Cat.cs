

using UnityEngine;

public class Cat : Animal, IVoicable
{
    public Cat mother;
    public Animal enemy;
    


    public Cat(int age, string name):base(age, name)
    {
        this.age = age;
        this.name = name;
    }

    public void Voice()
    {
        Debug.Log("Mew");
    }


}
