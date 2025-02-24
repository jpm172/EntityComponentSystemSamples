using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chest : Limb
{

    public Chest( BodyPart bodyPart, Image img, LimbStatusMeter meter, BodyHealthManager bodyManager, int maxHealth ) : base( bodyPart, img, meter, bodyManager, maxHealth )
    {
        _wounds = new List<Wound>();
        _manager = PlayerUIManager.Instance;
        _bleed = 0;
        _bodyPart = bodyPart;
        _image = img;
        _maxHealth = maxHealth;
        _currentHealth = 0;
        _meter = meter;
        meter.Initialize( this );
    }

    /*
    protected override float GetBleed()
    {
        float amt = _bleed;
        foreach ( Limb limb in _bodyManager.BodyParts.Values )
        {
            if(limb.BodyPart == BodyPart.Chest)
                continue;
            if ( limb.Destroyed )
                amt += limb.Bleed;
        }

        return amt;
    }
    */

    protected override bool IsHealthy()
    {
        return _currentHealth >= 0;
    }

    protected override int GetMissingHealth()
    {
        return Math.Abs( _currentHealth );
    }

    public override void Damage( WoundInfo info )
    {
        int clampedDamage = Math.Min( info.Damage, _manager.PlayerCurrentHealth );
        _manager.PlayerCurrentHealth -=clampedDamage;
        _currentHealth -= clampedDamage;
        _bleed += info.Bleed;
        
        if(_manager.PlayerCurrentHealth <= 0)
            DestroyLimb();
        
        _image.color = Color.Lerp( Color.black, Color.white, 1 - Math.Abs((float)_currentHealth/_maxHealth) );   
        _meter.UpdateStatus( this );
    }
    
    public override void Damage( int damage )
    {
        int clampedDamage = Math.Min( damage, _manager.PlayerCurrentHealth );
        _manager.PlayerCurrentHealth -= clampedDamage;
        _currentHealth -= clampedDamage;
        
        if(_manager.PlayerCurrentHealth <= 0)
            DestroyLimb();//

        _image.color = Color.Lerp( Color.black, Color.white, 1 - Math.Abs((float)_currentHealth/_maxHealth) );   
        _meter.UpdateStatus( this );
    }

    public override void Heal( int amount )
    {
        _manager.PlayerCurrentHealth += amount;
        _currentHealth += amount;
        
        if(_destroyed && _manager.PlayerCurrentHealth > 0)
            ReviveLimb();
        
        _image.color = Color.Lerp( Color.black, Color.white, 1 - Math.Abs((float)_currentHealth/_maxHealth) );
        _meter.UpdateStatus( this );
    }
    
    public override void Heal( int healAmount, float bleedHealAmount)
    {
        _manager.PlayerCurrentHealth += healAmount;
        _currentHealth += healAmount;
        _bleed = Math.Max( 0, _bleed - bleedHealAmount );
        
        if ( _bleed <= MIN_BLEED )
            _bleed = 0;
        
        if(_destroyed && _manager.PlayerCurrentHealth > 0)
            ReviveLimb();
        
        _image.color = Color.Lerp( Color.black, Color.white, 1 - Math.Abs((float)_currentHealth/_maxHealth) );
        _meter.UpdateStatus( this );
    }

    public override bool ShouldSpreadDamage( int damage )
    {
        return false;
    }
    
}
