public enum TestEnum
{
    Rock,
    Spear
}

public class TestBehaviour : UnityEngine.MonoBehaviour
{
    public static void Test()
    {
        new UnityEngine.GameObject("testing").AddComponent<TestBehaviour>();
    }
    
    public void Start()
    {
        PastebinMachine.EnumExtender.EnumExtender.Test();
    }
}