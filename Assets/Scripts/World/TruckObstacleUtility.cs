using MonsterMiner.Player;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class TruckObstacleUtility
    {
        public static bool TryGetTree(Collider hit, out PlainsTreeObstacle tree)
        {
            tree = null;
            if (hit == null)
                return false;

            tree = hit.GetComponentInParent<PlainsTreeObstacle>();
            if (tree != null)
                return true;

            Transform root = FindNamedRoot(hit.transform, "Tree_");
            if (root == null)
                return false;

            tree = root.GetComponent<PlainsTreeObstacle>();
            if (tree == null)
                tree = root.gameObject.AddComponent<PlainsTreeObstacle>();

            tree.EnsureCollider();
            var treeCollider = tree.GetComponent<Collider>();
            if (treeCollider != null)
                DriveableTruck.RegisterPassThroughObstacle(treeCollider);
            return true;
        }

        public static bool TryGetRock(Collider hit, out PlainsRockObstacle rock)
        {
            rock = null;
            if (hit == null)
                return false;

            rock = hit.GetComponentInParent<PlainsRockObstacle>();
            if (rock != null)
                return true;

            Transform root = FindNamedRoot(hit.transform, "PlainsRock_");
            if (root == null)
                return false;

            rock = root.GetComponent<PlainsRockObstacle>();
            if (rock == null)
                rock = root.gameObject.AddComponent<PlainsRockObstacle>();

            rock.EnsureCollider();
            return true;
        }

        static Transform FindNamedRoot(Transform start, string namePrefix)
        {
            Transform current = start;
            while (current != null)
            {
                if (current.name.StartsWith(namePrefix))
                    return current;

                current = current.parent;
            }

            return null;
        }
    }
}
