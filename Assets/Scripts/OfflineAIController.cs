using System.Collections;
using UnityEngine;

public sealed class OfflineAIController : MonoBehaviour
{
    private const int SlotCount = 5;
    private const int MaximumPlaysPerTurn = 5;

    [SerializeField] private float thinkingDelay = 0.9f;
    [SerializeField] private float moveDelay = 0.75f;

    private PlayerManager ai;
    private PlayerManager human;
    private TurnManager turnManager;
    private bool playingTurn;

    private readonly struct PlayCandidate
    {
        public readonly int handIndex;
        public readonly int fieldIndex;
        public readonly int manaCost;
        public readonly float score;

        public PlayCandidate(int handIndex, int fieldIndex, int manaCost, float score)
        {
            this.handIndex = handIndex;
            this.fieldIndex = fieldIndex;
            this.manaCost = manaCost;
            this.score = score;
        }
    }

    public void Initialize(PlayerManager aiPlayer, PlayerManager humanPlayer, TurnManager battleTurnManager)
    {
        ai = aiPlayer;
        human = humanPlayer;
        turnManager = battleTurnManager;
    }

    private void Update()
    {
        if (!playingTurn && ai && human && turnManager && ai.isOurTurn && ai.hp > 0 && human.hp > 0)
            StartCoroutine(PlayTurn());
    }

    private IEnumerator PlayTurn()
    {
        playingTurn = true;

        while (ai && ai.isResolvingActions)
            yield return null;

        yield return new WaitForSeconds(thinkingDelay);

        if (!CanContinueTurn())
        {
            playingTurn = false;
            yield break;
        }

        if (TryMakeBestFusion())
            yield return new WaitForSeconds(moveDelay);

        int plays = 0;
        while (CanContinueTurn() && plays < MaximumPlaysPerTurn)
        {
            PlayCandidate candidate = FindBestPlay();
            if (candidate.handIndex < 0 || candidate.score <= 0f)
                break;

            ExecutePlay(candidate);
            plays++;
            yield return new WaitForSeconds(moveDelay);
        }

        if (CanContinueTurn())
        {
            yield return ai.ResolveEndTurn();
            yield return new WaitForSeconds(moveDelay * 0.5f);
            turnManager.EndTurn(ai, human);
        }

        playingTurn = false;
    }

    private bool CanContinueTurn()
    {
        return ai && human && ai.isOurTurn && ai.hp > 0 && human.hp > 0;
    }

    private PlayCandidate FindBestPlay()
    {
        PlayCandidate best = new PlayCandidate(-1, -1, 0, float.NegativeInfinity);

        for (int handIndex = 0; handIndex < SlotCount; handIndex++)
        {
            HandCard handCard = ai.hand[handIndex];
            if (!handCard)
                continue;

            CardInfo card = handCard.cardData;
            for (int fieldIndex = 0; fieldIndex < SlotCount; fieldIndex++)
            {
                FieldCard existing = ai.field[fieldIndex];
                if (existing && existing.frozenTimer > 0)
                    continue;

                int fieldSpirit = existing ? existing.spr : 0;
                int manaCost = Mathf.Max(0, card.spr - fieldSpirit);
                if (manaCost > ai.sp)
                    continue;

                float score = EvaluatePlay(card, fieldIndex, manaCost, existing);
                score += Random.Range(-0.08f, 0.08f);
                if (score > best.score)
                    best = new PlayCandidate(handIndex, fieldIndex, manaCost, score);
            }
        }

        return best;
    }

    private float EvaluatePlay(CardInfo card, int slot, int manaCost, FieldCard replaced)
    {
        float score = 1.25f + card.spr * 0.7f - manaCost * 1.15f;
        if (replaced)
            score -= EstimateBoardValue(replaced.cardData) * 0.8f;

        FieldCard opposingCard = human.field[slot];
        int missingHealth = Mathf.Max(0, 20 - ai.hp);

        switch ((FieldCard.Ability)card.ability)
        {
            case FieldCard.Ability.Damage:
                int damage = EstimateDamage(card, slot);
                score += damage * 1.65f;
                if (damage >= human.hp)
                    score += 100f;
                break;

            case FieldCard.Ability.Heal:
                score += Mathf.Min(missingHealth, card.spr) * 1.35f;
                break;

            case FieldCard.Ability.Summoning:
                score += card.spr * 1.7f;
                break;

            case FieldCard.Ability.DrainLife:
                score += Mathf.Min(card.spr, human.hp) * 1.8f;
                if (card.spr >= human.hp)
                    score += 100f;
                break;

            case FieldCard.Ability.StealLife:
                score += Mathf.Min(1, human.hp) * 2.4f;
                break;

            case FieldCard.Ability.DrainMana:
                score += Mathf.Min(card.spr, human.sp) * 1.5f;
                break;

            case FieldCard.Ability.StealMana:
                score += human.sp > 0 ? 2.5f : 0f;
                break;

            case FieldCard.Ability.Draw:
                score += card.spr * 1.2f;
                break;

            case FieldCard.Ability.ManaBoost:
                score += card.spr * 1.35f;
                break;

            case FieldCard.Ability.Freeze:
                score += opposingCard ? 4f + opposingCard.spr : -8f;
                break;

            case FieldCard.Ability.Rot:
                score += opposingCard ? 3.5f + opposingCard.spr : -7f;
                break;

            case FieldCard.Ability.Blitz:
                score += opposingCard ? 5f + EstimateBoardValue(opposingCard.cardData) : -8f;
                break;

            case FieldCard.Ability.Swap:
                score += opposingCard ? EstimateBoardValue(opposingCard.cardData) - card.spr : -5f;
                break;

            case FieldCard.Ability.Sight:
                score += human.hand[slot] ? 1.5f : -2f;
                break;

            case FieldCard.Ability.Sacrifice:
                score += Mathf.Min(missingHealth, card.spr * 2) * 1.2f - card.spr * 0.4f;
                break;

            case FieldCard.Ability.Defend:
                score += CountOtherFriendlyCards(slot) * 0.85f;
                break;

            case FieldCard.Ability.ClearField:
                score += CountCards(human.field) * 2.5f - CountCards(ai.field) * 1.8f;
                break;

            case FieldCard.Ability.ClearBoard:
                score += (CountCards(human.field) + CountCards(human.hand)) * 2f;
                score -= (CountCards(ai.field) + CountCards(ai.hand)) * 1.6f;
                break;

            case FieldCard.Ability.DeckBurn:
                score += Mathf.Min(human.deckSize, card.spr / 2) * 0.9f;
                break;

            case FieldCard.Ability.ConvertToMana:
                score += EstimateHandManaValue() * 0.7f;
                break;

            case FieldCard.Ability.Evolve:
            case FieldCard.Ability.Duplicate:
            case FieldCard.Ability.Spawn:
            case FieldCard.Ability.DeckCard:
            case FieldCard.Ability.Luck:
                score += 1.5f;
                break;

            case FieldCard.Ability.Bomb:
                score -= card.spr * 1.4f;
                break;

            case FieldCard.Ability.ReturnToDeck:
                score -= CountCards(ai.hand) * 0.4f;
                break;
        }

        return score;
    }

    private void ExecutePlay(PlayCandidate candidate)
    {
        HandCard handCard = ai.hand[candidate.handIndex];
        if (!handCard)
            return;

        CardInfo card = handCard.cardData;
        ai.RequestDestroyHandCard(candidate.handIndex);
        if (candidate.manaCost > 0)
            ai.RequestAdjustMana(-candidate.manaCost);
        if (ai.field[candidate.fieldIndex])
            ai.RequestDestroyFieldCard(candidate.fieldIndex);
        ai.RequestPlayCard(card, candidate.fieldIndex);
    }

    private bool TryMakeBestFusion()
    {
        int bestSource = -1;
        int bestTarget = -1;
        float bestScore = 0f;

        for (int source = 0; source < SlotCount; source++)
        {
            FieldCard sourceCard = ai.field[source];
            if (!sourceCard || sourceCard.frozenTimer > 0 || sourceCard.cardData.spawn == null ||
                string.IsNullOrWhiteSpace(sourceCard.cardData.fusion))
                continue;

            for (int target = 0; target < SlotCount; target++)
            {
                FieldCard targetCard = ai.field[target];
                if (target == source || !targetCard || targetCard.frozenTimer > 0 ||
                    sourceCard.cardData.fusion != targetCard.title)
                    continue;

                CardInfo fusion = new CardInfo(sourceCard.cardData.spawn);
                float score = EstimateBoardValue(fusion) -
                              EstimateBoardValue(sourceCard.cardData) -
                              EstimateBoardValue(targetCard.cardData) * 0.5f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSource = source;
                    bestTarget = target;
                }
            }
        }

        if (bestSource < 0)
            return false;

        string sourceName = ai.field[bestSource].title;
        string targetName = ai.field[bestTarget].title;
        CardInfo evolvedCard = new CardInfo(ai.field[bestSource].cardData.spawn);
        ai.RecordAction($"{ai.actionName} fuses <b>{sourceName}</b> with <b>{targetName}</b> into <b>{evolvedCard.title}</b>.");
        ai.RequestDestroyFieldCard(bestSource);
        ai.RequestDestroyFieldCard(bestTarget);
        ai.RequestPlayCard(evolvedCard, bestTarget);
        return true;
    }

    private int EstimateDamage(CardInfo card, int sourceSlot)
    {
        int[] pattern = card.attackPattern;
        if (pattern == null || pattern.Length < SlotCount)
            return 0;

        int total = 0;
        for (int offset = -2; offset <= 2; offset++)
        {
            int targetSlot = sourceSlot + offset;
            if (targetSlot < 0 || targetSlot >= SlotCount)
                continue;

            int attack = pattern[offset + 2];
            if (attack <= 0)
                continue;

            FieldCard defender = human.field[targetSlot];
            total += defender && defender.frozenTimer == 0
                ? Mathf.Max(0, attack - defender.spr - defender.defense)
                : attack;
        }

        return total;
    }

    private static float EstimateBoardValue(CardInfo card)
    {
        float value = 1f + card.spr;
        FieldCard.Ability ability = (FieldCard.Ability)card.ability;
        if (ability == FieldCard.Ability.Damage && card.attackPattern != null)
        {
            for (int i = 0; i < card.attackPattern.Length; i++)
                value += Mathf.Max(0, card.attackPattern[i]) * 0.25f;
        }
        else if (ability == FieldCard.Ability.Summoning || ability == FieldCard.Ability.Draw ||
                 ability == FieldCard.Ability.DrainLife || ability == FieldCard.Ability.Defend)
        {
            value += 1.5f;
        }

        return value;
    }

    private int EstimateHandManaValue()
    {
        int value = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (ai.hand[i])
                value += ai.hand[i].spr / 2;
        }

        return value;
    }

    private int CountOtherFriendlyCards(int excludedSlot)
    {
        int count = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (i != excludedSlot && ai.field[i])
                count++;
        }

        return count;
    }

    private static int CountCards<T>(T[] cards) where T : Object
    {
        int count = 0;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i])
                count++;
        }

        return count;
    }
}
