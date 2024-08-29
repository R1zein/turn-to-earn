using UnityEngine;

public class Dog : Animal, IVoicable
{ 
    public Dog mother;
    public Cat enemy;

    public Dog(int age, string name) : base(age, name)
    {

    }

    public void Voice()
    {
        Debug.Log("Gav");
    }
}
