using UnityEngine;

public class Mouse : Animal, IVoicable
{
    public Mouse(int age, string name) : base(age, name)
    {

    }

    public  void Voice()
    {
        Debug.Log("Pisk");
    }


}
