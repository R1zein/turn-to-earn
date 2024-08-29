using System;
using UnityEngine;

public class BootStrap : MonoBehaviour
{
    public Invoker ability;
    void Start()
    {

       // Dog dog1 = new Dog(5, "Tuzik");
       // Dog dog2 = new Dog(2, "Bobik");
       //
       // Cat cat1 = new Cat(5, "Foxik");
       // Cat cat2 = new Cat(8, "Qwerty");
       // Cat cat3 = new Cat(1, "Pudge");
       //
       // cat3.mother = cat2;
       //
       // cat3.enemy = dog1;
       // cat1.enemy = dog2;
       // cat2.enemy = dog1;
       // Array values = Enum.GetValues(typeof(Invoker));
       // var random = UnityEngine.Random.Range(0, values.Length);
       // ability = (Invoker)values.GetValue(random);
       // CastSpell();  

        Node main = new Node();
        ActionStrategy justDo = new ActionStrategy(CastSpell);
        Leaf greenLeaf = new Leaf("greenLeaf", justDo);
        main.AddChild(greenLeaf);
        main.Process();
    }

    private void Update()
    {
        
    }

    private void CastSpell()
    {
        switch(ability)
        {
            case Invoker.eee:
               print("sunstrike");
               break;
            case Invoker.qqq:
                print("cold snap");
                break;
            case Invoker.qqe:
                print("ice wall");
                break;
            default:
                print("other spell");
                break;

        }
    }
}



public enum Invoker
{
    qqq,
    qqw,
    qqe,
    www,
    wwq,
    wwe,
    eee,
    eeq,
    eew,
    qwe,

}
