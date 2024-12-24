using System;

public enum LevelMaterial: byte
{
    None = 0,
    Drywall = 1,
    Brick = 2,
    Indestructible = byte.MaxValue - 1,
    //the editor does display MaxValue in GUI for some reason, but subtracting one fixes this
    //using MaxValue still works in code, but just doesnt display in GUI menus
}