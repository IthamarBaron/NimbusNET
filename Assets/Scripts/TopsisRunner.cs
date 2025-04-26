using UnityEngine;

public class TopsisRunner
{
    //                       Price  Deviation  AC    ER    CT
    public float[] weights = { 0.05f, 0.20f, 0.3f, 0.2f, 0.25f };
    public bool[] isBenefits = { false, false, true, true, true };
    public static readonly int criterions = 5;

    //Given a threat and interceptors, return the best one
    public IInterceptor GetBestInterceptor(IInterceptor[] availableInterceptors, IThreat threat)
    {
        if (weights.Length != criterions || criterions != isBenefits.Length)
        {
            Debug.LogError("[TOPSIS] CANT RUN TOPSIS! [Criteria/Benefits] Settings are off!");
        }

        // 1. Build Evaluation Matrix
        float[,] decisionMatrix = BuildDecisionMatrix(availableInterceptors, threat);

        // 2. Normalize Matrix
        float[,] normalizedMatrix = NormalizeMatrix(decisionMatrix);

        // 3. Apply Weights
        float[,] weightedMatrix = ApplyWeights(normalizedMatrix, weights);

        // 4. Determine Ideal & Anti-Ideal Solutions
        (float[] ideal, float[] antiIdeal) = GetIdealAndAntiIdeal(weightedMatrix, isBenefits);

        // 5. Calculate Distances
        float[] distancesToIdeal = CalcDistanceTo(weightedMatrix, ideal);
        float[] distancesToAntiIdeal = CalcDistanceTo(weightedMatrix, antiIdeal);

        // 6. Get TOPSIS Scores
        float[] scores = CalcTopsisScores(distancesToIdeal, distancesToAntiIdeal);

        // 7. Select Best Interceptor
        int bestIndex = SelectBest(scores);
        Debug.Log($"[TOPSIS] Best interceptor: {availableInterceptors[bestIndex].GetGameObject().name}");
        return availableInterceptors[bestIndex];

    }

    // ========== STEP ==========

    // Step 1: Evaluation Matrix
    private float[,] BuildDecisionMatrix(IInterceptor[] interceptors, IThreat threat)
    {
        float[,] decisionMatrix = new float[interceptors.Length,criterions];

        for (int i = 0; i < interceptors.Length; i++)
        {
            decisionMatrix[i, 0] = interceptors[i].interceptionPrice;
            decisionMatrix[i, 1] = CriteriaCalculator.Instance.CalcPathDeviation(interceptors[i], threat);
            decisionMatrix[i, 2] = CriteriaCalculator.Instance.CalcAltCompatibility(interceptors[i], threat);
            float _ER = CriteriaCalculator.Instance.CalcEffectiveRange(interceptors[i], threat);
            decisionMatrix[i, 3] = interceptors[i].type != "sam"? _ER: 1000-_ER;
            decisionMatrix[i, 4] = CriteriaCalculator.Instance.CalcCriticalTime(interceptors[i], threat);
            if (interceptors[i].type != "sam" && (1000 - _ER) < 0)
            {
                Debug.LogWarning($"[TOPSIS] EffeciveRange to intercptor {interceptors[i].GetGameObject().name} is bigger then a 1000 \n !!!NEGATIVE RANGE!!!");
            }
        }

        return decisionMatrix; 
    }

    // Step 2: Normalization
    private float[,] NormalizeMatrix(float[,] matrix) 
    {
        float[] norms = new float[criterions];
        for (int i = 0; i < matrix.GetLength(1); i++)
        {
            float collSquaredSum = 0f;
            for (int j = 0; j < matrix.GetLength(0); j++)
            {

                collSquaredSum += matrix[j, i] * matrix[j, i];
            }
            norms[i] = Sqrt(collSquaredSum);
        }

        for (int i = 0; i < matrix.GetLength(1); i++)
        {
            for (int j = 0; j < matrix.GetLength(0); j++)
            {
                if (norms[i] !=0) //avoid NaN
                {
                    matrix[j, i] = matrix[j, i] / norms[i];
                }
                else
                {
                    matrix[j, i] = 0;
                }
            }
        }

        return matrix;
    }

    // Step 3: Weights
    private float[,] ApplyWeights(float[,] matrix, float[] weights) 
    {

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                matrix[i,j] = matrix[i,j] * weights[j];
            }
        }
        return matrix;
    }

    // Step 4: Ideal / Anti-Ideal
    private (float[] ideal, float[] antiIdeal) GetIdealAndAntiIdeal(float[,] matrix, bool[] isBenefit)
    {
        float[] ideal = new float[criterions];
        float[] antiIdeal= new float[criterions];
        for (int i = 0; i < matrix.GetLength(1); i++)
        {
            //find max and min in each coll
            float collMin = matrix[0, i];
            float collMax = matrix[0, i];
            for (int j = 0; j < matrix.GetLength(0); j++)
            {
                if (matrix[j, i] < collMin)
                {
                    collMin= matrix[j, i];
                }
                if (matrix[j,i] > collMax)
                {
                    collMax= matrix[j,i];
                }
            }

            //Set Ideal and Anti-Ideal
            if (isBenefit[i])
            {
                ideal[i] = collMax;
                antiIdeal[i] = collMin;
            }
            else
            {
                ideal[i] = collMin;
                antiIdeal[i] = collMax;
            }
        }

        return (ideal, antiIdeal);
    }

    // Step 5: Euclidean Distance
    private float[] CalcDistanceTo(float[,] matrix, float[] targetVector)
    {
        float[] distances = new float[matrix.GetLength(0)];

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            float sumOfDistSquared = 0f;
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                float diff = (matrix[i, j] - targetVector[j]);
                sumOfDistSquared += diff*diff;
            }
            distances[i] = Sqrt(sumOfDistSquared);
        }
        return distances;
    }

    // Step 6: TOPSIS Scores (place BEST AT [0])
    private float[] CalcTopsisScores(float[] DPlus, float[] DMinus)
    {

        float[] scores = new float[DPlus.Length];
        for (int i = 0; i < scores.Length; i++)
        {
            float denominator = DPlus[i] + DMinus[i];
            scores[i] = (denominator == 0f) ? 0f : DMinus[i] / denominator;
        }

        return scores;
    }

    // Step 7: Selection
    private int SelectBest(float[] topsisScores)
    {
        int maxScoreIndex = 0;
        for (int i = 0; i < topsisScores.Length; i++)
        {
            if (topsisScores[i] > topsisScores[maxScoreIndex])
            {
                maxScoreIndex = i;
            }
        }

        return maxScoreIndex;
    }

    float Sqrt(float x)
    {
        if (x <= 0.0f) return 0.0f;

        float guess = x;
        float epsilon = 0.0001f;

        for (int i = 0; i < 200; i++)
        {
            float newGuess = 0.5f * (guess + x / guess);

            if ((newGuess - guess) < epsilon && (newGuess - guess) > -epsilon)
            {
                return newGuess;
            }

            guess = newGuess;
        }
        Debug.LogWarning($"Sqrt Fail-Saif triggred for {x} gave result {guess}");
        return guess;

    }

}
