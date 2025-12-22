using System;
using System.Collections.Generic;

[Serializable]
public class MenuNode
{
    public string id;
    public string label;

    /// <summary>
    /// If actionId is set and children is empty, this is a leaf action button.
    /// If children is not empty, this node navigates to its submenu.
    /// </summary>
    public string actionId;

    public List<MenuNode> children = new List<MenuNode>();
}
