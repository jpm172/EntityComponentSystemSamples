using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct CharacterBody : IComponentData
{
    public CharacterLimb Head;
    public CharacterLimb Chest;
    public CharacterLimb LeftArm;
    public CharacterLimb RightArm;
    public CharacterLimb LeftLeg;
    public CharacterLimb RightLeg;


    public CharacterLimb GetLimb( BodyPart limb )
    {
        switch ( limb )
        {
            case BodyPart.Head:
                return Head;
            case BodyPart.Chest:
                return Chest;
            case BodyPart.LeftArm:
                return LeftArm;
            case BodyPart.RightArm:
                return RightArm;
            case BodyPart.LeftLeg:
                return LeftLeg;
            case BodyPart.RightLeg:
                return RightLeg;
            default:
                return Chest;
        }
    }
}
