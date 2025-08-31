using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyLabel : MonoBehaviour
{
    [SerializeField]
    private BodyPart _bodyPart;

    public BodyPart BodyPart => _bodyPart;
}
