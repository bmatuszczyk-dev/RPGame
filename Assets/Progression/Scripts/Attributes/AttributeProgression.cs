using System;
using System.Collections.Generic;
using RPGame.Core.Statistics.Attributes;

namespace RPGame.Progression
{
    public sealed class AttributeProgression
    {
        private const int BaseCost = 100;
        private const int CostIncreasePerPurchasedPoint = 50;

        private readonly CharacterAttributes attributes;
        private readonly Func<int> getAvailableXP;
        private readonly Action<int> spendXP;

        public event Action<IReadOnlyList<AttributePurchase>> PointsPurchased;

        public AttributeProgression(
            CharacterAttributes attributes,
            Func<int> getAvailableXP,
            Action<int> spendXP)
        {
            this.attributes = attributes;
            this.getAvailableXP = getAvailableXP;
            this.spendXP = spendXP;
        }

        public int GetNextPointCost(CharacterAttributeType attributeType)
        {
            return GetPointCost(attributes != null ? attributes.GetPurchasedPoints(attributeType) : 0);
        }

        public int GetTotalCost(IReadOnlyDictionary<CharacterAttributeType, int> pendingPoints)
        {
            if (pendingPoints == null || attributes == null)
            {
                return 0;
            }

            int totalCost = 0;
            foreach (KeyValuePair<CharacterAttributeType, int> pendingPoint in pendingPoints)
            {
                int pointsToBuy = Math.Max(0, pendingPoint.Value);
                int purchasedPoints = attributes.GetPurchasedPoints(pendingPoint.Key);
                for (int i = 0; i < pointsToBuy; i++)
                {
                    totalCost += GetPointCost(purchasedPoints + i);
                }
            }

            return totalCost;
        }

        public bool CanBuyAttributePoint(CharacterAttributeType attributeType)
        {
            return attributes != null && GetNextPointCost(attributeType) <= getAvailableXP();
        }

        public AttributePurchaseResult TryBuyAttributePoint(CharacterAttributeType attributeType)
        {
            Dictionary<CharacterAttributeType, int> pendingPoints = new()
            {
                { attributeType, 1 }
            };

            return TryBuyAttributePoints(pendingPoints);
        }

        public AttributePurchaseResult TryBuyAttributePoints(IReadOnlyDictionary<CharacterAttributeType, int> pendingPoints)
        {
            if (attributes == null || pendingPoints == null)
            {
                return new AttributePurchaseResult(success: false, spentXP: 0);
            }

            int totalCost = GetTotalCost(pendingPoints);
            if (totalCost <= 0 || totalCost > getAvailableXP())
            {
                return new AttributePurchaseResult(success: false, spentXP: 0);
            }

            spendXP(totalCost);
            List<AttributePurchase> purchases = new();
            foreach (KeyValuePair<CharacterAttributeType, int> pendingPoint in pendingPoints)
            {
                int pointsToBuy = Math.Max(0, pendingPoint.Value);
                if (pointsToBuy > 0)
                {
                    int previousPurchasedPoints = attributes.GetPurchasedPoints(pendingPoint.Key);
                    int previousValue = attributes.GetValue(pendingPoint.Key);
                    int cost = GetPointsCost(previousPurchasedPoints, pointsToBuy);
                    attributes.AddPurchasedPoints(pendingPoint.Key, pointsToBuy);
                    purchases.Add(new AttributePurchase(
                        pendingPoint.Key,
                        pointsToBuy,
                        previousPurchasedPoints,
                        attributes.GetPurchasedPoints(pendingPoint.Key),
                        previousValue,
                        attributes.GetValue(pendingPoint.Key),
                        cost));
                }
            }

            PointsPurchased?.Invoke(purchases);

            return new AttributePurchaseResult(success: true, spentXP: totalCost);
        }

        private int GetPointsCost(int purchasedPoints, int pointsToBuy)
        {
            int cost = 0;
            for (int i = 0; i < pointsToBuy; i++)
            {
                cost += GetPointCost(purchasedPoints + i);
            }

            return cost;
        }

        private int GetPointCost(int purchasedPoints)
        {
            return BaseCost + Math.Max(0, purchasedPoints) * CostIncreasePerPurchasedPoint;
        }
    }

    public readonly struct AttributePurchase
    {
        public AttributePurchase(
            CharacterAttributeType attributeType,
            int points,
            int previousPurchasedPoints,
            int currentPurchasedPoints,
            int previousValue,
            int currentValue,
            int cost)
        {
            AttributeType = attributeType;
            Points = points;
            PreviousPurchasedPoints = previousPurchasedPoints;
            CurrentPurchasedPoints = currentPurchasedPoints;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            Cost = cost;
        }

        public CharacterAttributeType AttributeType { get; }
        public int Points { get; }
        public int PreviousPurchasedPoints { get; }
        public int CurrentPurchasedPoints { get; }
        public int PreviousValue { get; }
        public int CurrentValue { get; }
        public int Cost { get; }
    }
}
