using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
public class Gun : ScriptableObject
{
    #region Variables

    public string name;
    public int damage;
    public int ammo;
    public float burst; //0: Semi, 1: Auto, 2+: Burst
    public int clipSize;
    public float firerate;
    public float bloom;
    public float recoil;
    public float kickback;
    public float aimSpeed;
    public float reload;
    public GameObject prefab;

    private int stash; // Current Ammo
    private int clip; // Current Bullets in Clip
    
    public void Initialize()
    {
        stash = ammo;
        clip = clipSize;
    }

    public bool FireBullet()
    {
        if (clip > 0)
        {
            clip -= 1;
            return true;
        }
        else return false;
    }

    public void Reload()
    {
        stash += clip;
        clip = Mathf.Min(clipSize, stash);
        stash -= clip;
    }

    public int GetStash() { return stash; }
    public int GetClip() { return clip; }
    #endregion
}
