using UnityEngine;

// Lets the player walk through bodies (enemies, bosses, dropped coins) instead of standing on them or pushing them,
// without a dedicated physics layer. Only solid colliders are ignored: an ignored pair gets no trigger messages
// either, so a Hurtbox or a pickup trigger keeps touching the player.
public static class PlayerPassThrough
{
    public static void Ignore(Collider2D[] own, Collider2D[] player)
    {
        if (own == null || player == null)
            return;
        foreach (Collider2D theirs in player)
        {
            if (theirs == null)
                continue;
            foreach (Collider2D mine in own)
                if (mine != null && !mine.isTrigger)
                    Physics2D.IgnoreCollision(mine, theirs);
        }
    }

    // Unity forgets ignored pairs when a collider is switched off and on (an enemy dying and coming back), so call
    // this every physics step for bodies that can be switched off: it ignores them again when needed.
    public static void Keep(Collider2D[] own, Collider2D[] player)
    {
        if (own == null || player == null)
            return;
        Collider2D probe = null;
        foreach (Collider2D mine in own)
            if (mine != null && !mine.isTrigger)
            {
                probe = mine;
                break;
            }
        if (probe == null)
            return;
        foreach (Collider2D theirs in player)
            if (theirs != null && !Physics2D.GetIgnoreCollision(probe, theirs))
            {
                Ignore(own, player);
                return;
            }
    }
}
