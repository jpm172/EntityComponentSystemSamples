using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Limb
{

    protected const float MIN_BLEED = 0.001f;
    
    [SerializeField]
    protected BodyPart _bodyPart;
    protected Image _image;
    protected float _maxHealth;
    protected float _currentHealth;

    protected BodyHealthManager _bodyManager;
    
    [SerializeField]
    protected List<Wound> _wounds;
    
    [SerializeField]
    protected float _bleed;
    protected LimbStatusMeter _meter;
    protected PlayerUIManager _manager;
    [SerializeField]
    protected float _cumulativeDamage;

    [SerializeField]
    protected bool _destroyed;

    public Image Image => _image;

    public BodyPart BodyPart => _bodyPart;

    public float CurrentHealth => _currentHealth;

    public float MaxHealth => _maxHealth;

    public float MissingHealth => GetMissingHealth();

    public float Bleed => GetBleed();

    public bool Destroyed => _destroyed;

    public bool Healthy => IsHealthy();

    public List<Wound> Wounds => _wounds;

    public float CumulativeDamage
    {
        get => _cumulativeDamage;
        set => _cumulativeDamage = Math.Max(0, value);
    }

    public Limb( BodyPart bodyPart, Image img, LimbStatusMeter meter, BodyHealthManager bodyManager, int maxHealth )
    {
        _wounds = new List<Wound>();
        _manager = PlayerUIManager.Instance;
        _bodyManager = bodyManager;
        _bleed = 0;
        _destroyed = false;
        _bodyPart = bodyPart;
        _image = img;
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
        _meter = meter;
        meter.Initialize( this );
    }

    protected virtual float GetBleed()
    {
        return _bleed;
    }
    
    protected virtual bool IsHealthy()
    {
        return _currentHealth >= _maxHealth;
    }

    protected virtual float GetMissingHealth()//
    {
        return _maxHealth - _currentHealth;
    }

    public virtual void Damage( WoundInfo info )
    {
        float clampedDamage = Math.Min( info.Damage, _currentHealth );
        _manager.PlayerCurrentHealth -=clampedDamage;
        _currentHealth -= clampedDamage;
        _bleed += info.Bleed;
        
        if(!_destroyed && _currentHealth <= 0)
            DestroyLimb();
        
        _image.color = Color.Lerp( Color.black, Color.white, (float)_currentHealth/_maxHealth );   
        _meter.UpdateStatus( this );
    }
    
    
    public virtual void Damage( int damage )
    {
        float clampedDamage = Math.Min( damage, _currentHealth );
        _manager.PlayerCurrentHealth -= clampedDamage;
        _currentHealth -= clampedDamage;
        
        if(!_destroyed && _currentHealth <= 0)
            DestroyLimb();
        
        _image.color = Color.Lerp( Color.black, Color.white, (float)_currentHealth/_maxHealth );   
        _meter.UpdateStatus( this );
    }

    public virtual void Heal( int amount )
    {
        _manager.PlayerCurrentHealth += amount;
        _currentHealth += amount;
        
        if(_destroyed && _currentHealth > 0)
            ReviveLimb();
        
        _image.color = Color.Lerp( Color.black, Color.white, (float)_currentHealth/_maxHealth );
        _meter.UpdateStatus( this );
    }
    
    public virtual void Heal( int healAmount, float bleedHealAmount)
    {
        _manager.PlayerCurrentHealth += healAmount;
        _currentHealth += healAmount;
        _bleed = Math.Max( 0, _bleed - bleedHealAmount );

        if ( _bleed <= MIN_BLEED )
            _bleed = 0;
        
        if(_destroyed && _currentHealth > 0)
            ReviveLimb();
        
        _image.color = Color.Lerp( Color.black, Color.white, (float)_currentHealth/_maxHealth );
        _meter.UpdateStatus( this );
    }

    public void HealBleed( float bleedHealAmount )
    {
        _bleed = Math.Max( 0, _bleed - bleedHealAmount );
        
        if ( _bleed <= MIN_BLEED )
            _bleed = 0;
        
        _meter.UpdateStatus( this );
    }

    public virtual bool ShouldSpreadDamage( int damage )
    {
        return damage > _currentHealth;
    }
    
    public virtual void DestroyLimb()
    {
        _destroyed = true;
    }

    public virtual void ReviveLimb()
    {
        _destroyed = false;
    }

    
}
