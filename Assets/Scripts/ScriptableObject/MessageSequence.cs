using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Guide/MessageSequence")]
public class MessageSequence : ScriptableObject
{
    [TextArea(2, 5)] public List<string> messages;
}
