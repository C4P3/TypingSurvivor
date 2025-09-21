[CreateAssetMenu(fileName = "OxygenHealEffect", menuName = "Items/Effects/OxygenHeal")]
public class OxygenHealEffect : ScriptableObject, IItemEffect
{
    [SerializeField] private float _amount;

    public void Execute(Player user)
    {
        // GameManagerなどを経由して酸素を回復させる
        // GameManager.Instance.RecoverOxygen(_amount); 
    }
}