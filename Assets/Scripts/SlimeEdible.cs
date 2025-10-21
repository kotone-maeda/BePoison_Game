using UnityEngine;

public class SlimeEdible : Edible
{
    [SerializeField] SlimeController slime;

    void Reset()
    {
        if (!slime) slime = GetComponent<SlimeController>();
    }

    public override bool CanBeEaten(PlayerController eater)
    {
        return slime && slime.IsDead; // ← SlimeController に public bool IsDead { get; } を用意
    }
}
