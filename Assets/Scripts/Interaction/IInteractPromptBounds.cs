using UnityEngine;

namespace MonsterMiner.Interaction
{
    public interface IInteractPromptBounds
    {
        bool TryGetPromptScreenRect(Camera camera, out Rect guiRect);
    }
}
