using UnityEngine;

namespace MonsterMiner.Artillery
{
    public enum ArtilleryTargetKind
    {
        Fortress,
        Palace,
        Infantry,
        Cavalry,
        Catapult
    }

    public enum ArtilleryProjectileHitKind
    {
        None,
        TargetHit,
        UnitDestroyed
    }

    public struct ArtilleryProjectileHitResult
    {
        public ArtilleryProjectileHitKind Kind;
        public float ImpactCenterX;
        public float ImpactBottomY;
        public float TargetWidth;
        public float Depth;

        public bool StruckTarget =>
            Kind == ArtilleryProjectileHitKind.TargetHit
            || Kind == ArtilleryProjectileHitKind.UnitDestroyed;

        public static ArtilleryProjectileHitResult Miss =>
            new ArtilleryProjectileHitResult { Kind = ArtilleryProjectileHitKind.None };
    }

    public class ArtilleryHitTarget : MonoBehaviour
    {
        ArtillerySide side;
        ArtilleryTargetKind kind;
        float centerX;
        float centerY;
        float width;
        float height;
        float depth;
        int hitsRemaining;
        int maxHits;
        int infantrySlot = -1;
        bool destroyed;
        bool projectileHittable = true;

        public ArtillerySide Side => side;
        public ArtilleryTargetKind Kind => kind;
        public float CenterX => centerX;
        public float CenterY => centerY;
        public float Width => width;
        public float Height => height;
        public float BottomY => centerY - height * 0.5f;
        public float TopY => centerY + height * 0.5f;
        public float Depth => depth;
        public int InfantrySlot => infantrySlot;
        public bool IsDestroyed => destroyed;
        public bool IsProjectileHittable => !destroyed && projectileHittable;
        public int HitsRemaining => hitsRemaining;
        public int MaxHits => maxHits;

        public string GetDisplayName()
        {
            switch (kind)
            {
                case ArtilleryTargetKind.Fortress:
                    return "Fortress";
                case ArtilleryTargetKind.Palace:
                    return "Palace";
                case ArtilleryTargetKind.Infantry:
                    return "Infantry";
                case ArtilleryTargetKind.Cavalry:
                    return "Cavalry";
                case ArtilleryTargetKind.Catapult:
                    return "Catapult";
                default:
                    return kind.ToString();
            }
        }

        public static int GetHitsRequired(ArtilleryTargetKind targetKind)
        {
            switch (targetKind)
            {
                case ArtilleryTargetKind.Fortress:
                    return 5;
                case ArtilleryTargetKind.Palace:
                    return 4;
                case ArtilleryTargetKind.Infantry:
                    return 3;
                case ArtilleryTargetKind.Cavalry:
                    return 2;
                default:
                    return 1;
            }
        }

        public void Configure(
            ArtillerySide targetSide,
            ArtilleryTargetKind targetKind,
            float targetCenterX,
            float targetCenterY,
            float targetWidth,
            float targetHeight,
            float targetDepth,
            int squadSlot = -1)
        {
            side = targetSide;
            kind = targetKind;
            centerX = targetCenterX;
            centerY = targetCenterY;
            width = targetWidth;
            height = targetHeight;
            depth = targetDepth;
            infantrySlot = squadSlot;
            maxHits = GetHitsRequired(targetKind);
            hitsRemaining = maxHits;
            destroyed = false;
            projectileHittable = true;
        }

        public void SetProjectileHittable(bool hittable)
        {
            projectileHittable = hittable;
        }

        public void SetCenterPosition(float targetCenterX, float targetCenterY)
        {
            centerX = targetCenterX;
            centerY = targetCenterY;
            transform.localPosition = new Vector3(centerX, centerY, depth);
        }

        public bool OverlapsHorizontally(float otherCenterX, float otherWidth)
        {
            if (destroyed)
                return false;

            float combinedHalfWidth = (width + otherWidth) * 0.5f;
            return Mathf.Abs(centerX - otherCenterX) < combinedHalfWidth;
        }

        public bool ContainsPoint(float localX, float localY)
        {
            if (destroyed)
                return false;

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            return localX >= centerX - halfWidth
                && localX <= centerX + halfWidth
                && localY >= centerY - halfHeight
                && localY <= centerY + halfHeight;
        }

        public bool IntersectsSegment(float x0, float y0, float x1, float y1)
        {
            if (destroyed)
                return false;

            if (ContainsPoint(x0, y0) || ContainsPoint(x1, y1))
                return true;

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float minX = centerX - halfWidth;
            float maxX = centerX + halfWidth;
            float minY = centerY - halfHeight;
            float maxY = centerY + halfHeight;

            return SegmentIntersectsHorizontalEdge(x0, y0, x1, y1, minY, minX, maxX)
                || SegmentIntersectsHorizontalEdge(x0, y0, x1, y1, maxY, minX, maxX)
                || SegmentIntersectsVerticalEdge(x0, y0, x1, y1, minX, minY, maxY)
                || SegmentIntersectsVerticalEdge(x0, y0, x1, y1, maxX, minY, maxY);
        }

        public bool TryGetSegmentEntryT(float x0, float y0, float x1, float y1, out float entryT)
        {
            entryT = float.MaxValue;
            if (destroyed)
                return false;

            if (ContainsPoint(x0, y0))
            {
                entryT = 0f;
                return true;
            }

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float minX = centerX - halfWidth;
            float maxX = centerX + halfWidth;
            float minY = centerY - halfHeight;
            float maxY = centerY + halfHeight;
            bool found = false;

            found |= TryAddSegmentEdgeEntryT(x0, y0, x1, y1, minY, minX, maxX, true, ref entryT);
            found |= TryAddSegmentEdgeEntryT(x0, y0, x1, y1, maxY, minX, maxX, true, ref entryT);
            found |= TryAddSegmentEdgeEntryT(x0, y0, x1, y1, minX, minY, maxY, false, ref entryT);
            found |= TryAddSegmentEdgeEntryT(x0, y0, x1, y1, maxX, minY, maxY, false, ref entryT);

            if (ContainsPoint(x1, y1))
            {
                entryT = Mathf.Min(entryT, 1f);
                found = true;
            }

            return found;
        }

        static bool TryAddSegmentEdgeEntryT(
            float x0,
            float y0,
            float x1,
            float y1,
            float edgePrimary,
            float edgeMinSecondary,
            float edgeMaxSecondary,
            bool horizontalEdge,
            ref float bestEntryT)
        {
            float t;
            float secondary;
            if (horizontalEdge)
            {
                if (Mathf.Approximately(y0, y1))
                    return false;

                t = (edgePrimary - y0) / (y1 - y0);
                if (t < 0f || t > 1f)
                    return false;

                secondary = Mathf.Lerp(x0, x1, t);
            }
            else
            {
                if (Mathf.Approximately(x0, x1))
                    return false;

                t = (edgePrimary - x0) / (x1 - x0);
                if (t < 0f || t > 1f)
                    return false;

                secondary = Mathf.Lerp(y0, y1, t);
            }

            if (secondary < edgeMinSecondary || secondary > edgeMaxSecondary)
                return false;

            if (t < bestEntryT)
                bestEntryT = t;

            return true;
        }

        static bool SegmentIntersectsHorizontalEdge(
            float x0,
            float y0,
            float x1,
            float y1,
            float edgeY,
            float edgeMinX,
            float edgeMaxX)
        {
            if (Mathf.Approximately(y0, y1))
                return Mathf.Approximately(y0, edgeY) && SegmentsOverlap(x0, x1, edgeMinX, edgeMaxX);

            float t = (edgeY - y0) / (y1 - y0);
            if (t < 0f || t > 1f)
                return false;

            float x = Mathf.Lerp(x0, x1, t);
            return x >= edgeMinX && x <= edgeMaxX;
        }

        static bool SegmentIntersectsVerticalEdge(
            float x0,
            float y0,
            float x1,
            float y1,
            float edgeX,
            float edgeMinY,
            float edgeMaxY)
        {
            if (Mathf.Approximately(x0, x1))
                return Mathf.Approximately(x0, edgeX) && SegmentsOverlap(y0, y1, edgeMinY, edgeMaxY);

            float t = (edgeX - x0) / (x1 - x0);
            if (t < 0f || t > 1f)
                return false;

            float y = Mathf.Lerp(y0, y1, t);
            return y >= edgeMinY && y <= edgeMaxY;
        }

        static bool SegmentsOverlap(float a0, float a1, float b0, float b1)
        {
            float aMin = Mathf.Min(a0, a1);
            float aMax = Mathf.Max(a0, a1);
            return aMax >= b0 && aMin <= b1;
        }

        public bool ApplyHit()
        {
            if (destroyed)
                return false;

            hitsRemaining = Mathf.Max(0, hitsRemaining - 1);
            if (hitsRemaining > 0)
                return false;

            DestroyTarget();
            return true;
        }

        public void DestroyTarget()
        {
            if (destroyed)
                return;

            destroyed = true;
            var targetRenderer = GetComponent<MeshRenderer>();
            if (targetRenderer != null)
                targetRenderer.enabled = false;

            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }
    }
}
