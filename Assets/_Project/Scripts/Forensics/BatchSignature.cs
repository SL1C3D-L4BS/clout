using UnityEngine;
using System;
using System.Collections.Generic;
using Clout.Empire.Crafting;

namespace Clout.Forensics
{
    /// <summary>
    /// Step 12A — Forensic fingerprint attached to every crafted batch.
    ///
    /// Every product batch carries a 512-dimensional signature vector generated
    /// deterministically from facility seed, recipe hash, worker skill, and
    /// equipment configuration — plus random per-batch variance. This creates
    /// a unique forensic fingerprint that law enforcement can use to link
    /// seized product back to its origin facility.
    ///
    /// Signature structure:
    ///   [0..127]   = Facility fingerprint (from property/station ID)
    ///   [128..255] = Recipe fingerprint (from recipe + ingredients)
    ///   [256..383] = Operator fingerprint (worker skill XOR equipment config)
    ///   [384..511] = Random variance (per-batch noise)
    ///
    /// Propagation: CraftingStation → ProductInventory → DealRecord → ForensicLabAI
    /// </summary>
    [Serializable]
    public class BatchSignature
    {
        public const int VECTOR_SIZE = 512;
        public const int FACILITY_START = 0;
        public const int FACILITY_END = 128;
        public const int RECIPE_START = 128;
        public const int RECIPE_END = 256;
        public const int OPERATOR_START = 256;
        public const int OPERATOR_END = 384;
        public const int VARIANCE_START = 384;
        public const int VARIANCE_END = 512;

        // ─── Core Data ────────────────────────────────────────
        [SerializeField] private float[] _vector = new float[VECTOR_SIZE];
        [SerializeField] private string _batchId;
        [SerializeField] private int _facilitySeed;
        [SerializeField] private int _recipeHash;
        [SerializeField] private int _operatorHash;
        [SerializeField] private int _equipmentHash;
        [SerializeField] private float _randomVariance;
        [SerializeField] private float _creationTime;
        [SerializeField] private int _creationDay;
        [SerializeField] private int _scrubLevel;
        [SerializeField] private string _productId;
        [SerializeField] private float _quality;

        // ─── Properties ───────────────────────────────────────
        public float[] Vector => _vector;
        public string BatchId => _batchId;
        public int FacilitySeed => _facilitySeed;
        public int RecipeHash => _recipeHash;
        public int OperatorHash => _operatorHash;
        public float CreationTime => _creationTime;
        public int CreationDay => _creationDay;
        public int ScrubLevel => _scrubLevel;
        public string ProductId => _productId;
        public float Quality => _quality;
        public bool IsScrubbed => _scrubLevel > 0;

        /// <summary>Age in game days since creation.</summary>
        public float AgeDays(float currentTime, float gameDayDuration)
        {
            return gameDayDuration > 0 ? (currentTime - _creationTime) / gameDayDuration : 0f;
        }

        // ─── Generation ───────────────────────────────────────

        /// <summary>
        /// Generate a new batch signature from crafting parameters.
        /// Called by CraftingStation.CompleteBatch() or SignatureGenerator.
        /// </summary>
        public static BatchSignature Generate(
            string stationId,
            string propertyId,
            RecipeDefinition recipe,
            List<IngredientDefinition> usedAdditives,
            float quality,
            float workerSkill,
            float equipmentQualityBonus,
            int currentDay)
        {
            var sig = new BatchSignature();

            // Generate deterministic seeds
            sig._facilitySeed = HashString(stationId + "|" + (propertyId ?? "unknown"));
            sig._recipeHash = HashRecipe(recipe, usedAdditives);
            sig._operatorHash = HashFloat(workerSkill);
            sig._equipmentHash = HashFloat(equipmentQualityBonus);
            sig._randomVariance = UnityEngine.Random.Range(0f, 0.1f);

            // Metadata
            sig._batchId = $"BS_{sig._facilitySeed:X4}_{Time.time:F0}_{UnityEngine.Random.Range(0, 9999):D4}";
            sig._creationTime = Time.time;
            sig._creationDay = currentDay;
            sig._scrubLevel = 0;
            sig._productId = recipe != null && recipe.outputProduct != null
                ? recipe.outputProduct.productName : "unknown";
            sig._quality = quality;

            // Build 512-dimensional vector
            sig._vector = new float[VECTOR_SIZE];

            // [0..127] Facility fingerprint
            FillDeterministic(sig._vector, FACILITY_START, FACILITY_END, sig._facilitySeed);

            // [128..255] Recipe fingerprint
            FillDeterministic(sig._vector, RECIPE_START, RECIPE_END, sig._recipeHash);

            // [256..383] Operator fingerprint (skill XOR equipment)
            int operatorSeed = sig._operatorHash ^ sig._equipmentHash;
            FillDeterministic(sig._vector, OPERATOR_START, OPERATOR_END, operatorSeed);

            // [384..511] Random variance
            FillRandom(sig._vector, VARIANCE_START, VARIANCE_END, sig._randomVariance);

            // Normalize to unit vector
            Normalize(sig._vector);

            return sig;
        }

        /// <summary>
        /// Generate a minimal signature for test/debug purposes.
        /// </summary>
        public static BatchSignature GenerateTest(string facilityId, string productId)
        {
            var sig = new BatchSignature();
            sig._facilitySeed = HashString(facilityId);
            sig._recipeHash = HashString(productId);
            sig._batchId = $"TEST_{facilityId}_{Time.time:F0}";
            sig._creationTime = Time.time;
            sig._productId = productId;
            sig._quality = 0.5f;
            sig._vector = new float[VECTOR_SIZE];

            FillDeterministic(sig._vector, FACILITY_START, FACILITY_END, sig._facilitySeed);
            FillDeterministic(sig._vector, RECIPE_START, RECIPE_END, sig._recipeHash);
            FillDeterministic(sig._vector, OPERATOR_START, OPERATOR_END, sig._facilitySeed ^ sig._recipeHash);
            FillRandom(sig._vector, VARIANCE_START, VARIANCE_END, 0.05f);
            Normalize(sig._vector);

            return sig;
        }

        // ─── Scrubbing ────────────────────────────────────────

        /// <summary>
        /// Apply scrubbing noise to this signature. Each level injects progressively
        /// more noise into the deterministic portions, making the signature harder
        /// to trace back to its origin.
        ///
        /// Level 1: similarity drops from ~0.95 to ~0.80
        /// Level 2: similarity drops to ~0.65 (below cluster threshold)
        /// Level 3: signature effectively randomized
        /// </summary>
        public void ApplyScrub(int newScrubLevel, float noiseInjection)
        {
            if (newScrubLevel <= _scrubLevel) return;

            _scrubLevel = newScrubLevel;

            // Inject noise into the deterministic portions (facility + recipe + operator)
            // The more noise, the harder to trace
            for (int i = 0; i < VARIANCE_START; i++)
            {
                float noise = DeterministicRandom(i + _scrubLevel * 1000) * noiseInjection;
                _vector[i] += noise;
            }

            // Re-normalize
            Normalize(_vector);
        }

        // ─── Similarity ───────────────────────────────────────

        /// <summary>
        /// Compute cosine similarity between two signature vectors.
        /// Returns 0.0 (completely different) to 1.0 (identical).
        ///
        /// Thresholds:
        ///   > 0.95 : Same facility, same batch run
        ///   > 0.85 : Same facility, different batch
        ///   > 0.70 : Same recipe, different facility
        ///   < 0.70 : Unrelated
        /// </summary>
        public static float CosineSimilarity(BatchSignature a, BatchSignature b)
        {
            if (a == null || b == null) return 0f;
            return CosineSimilarity(a._vector, b._vector);
        }

        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 0f;

            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            float denominator = Mathf.Sqrt(magA) * Mathf.Sqrt(magB);
            return denominator > 0f ? dot / denominator : 0f;
        }

        /// <summary>
        /// Compute facility-only similarity (first 128 dimensions).
        /// Used for quick facility matching without full vector comparison.
        /// </summary>
        public static float FacilitySimilarity(BatchSignature a, BatchSignature b)
        {
            if (a == null || b == null) return 0f;
            return PartialCosineSimilarity(a._vector, b._vector, FACILITY_START, FACILITY_END);
        }

        /// <summary>
        /// Compute recipe-only similarity (dimensions 128-255).
        /// Used to identify same recipe across different facilities.
        /// </summary>
        public static float RecipeSimilarity(BatchSignature a, BatchSignature b)
        {
            if (a == null || b == null) return 0f;
            return PartialCosineSimilarity(a._vector, b._vector, RECIPE_START, RECIPE_END);
        }

        private static float PartialCosineSimilarity(float[] a, float[] b, int start, int end)
        {
            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = start; i < end; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            float denominator = Mathf.Sqrt(magA) * Mathf.Sqrt(magB);
            return denominator > 0f ? dot / denominator : 0f;
        }

        // ─── Hashing Helpers ──────────────────────────────────

        private static int HashString(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            // Simple deterministic hash (not crypto — just fingerprint)
            int hash = 17;
            for (int i = 0; i < s.Length; i++)
                hash = hash * 31 + s[i];
            return hash;
        }

        private static int HashRecipe(RecipeDefinition recipe, List<IngredientDefinition> additives)
        {
            if (recipe == null) return 0;
            int hash = HashString(recipe.recipeName);

            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Length; i++)
                {
                    if (recipe.ingredients[i].ingredient != null)
                        hash ^= HashString(recipe.ingredients[i].ingredient.ingredientName) * (i + 1);
                }
            }

            if (additives != null)
            {
                for (int i = 0; i < additives.Count; i++)
                {
                    if (additives[i] != null)
                        hash ^= HashString(additives[i].ingredientName) * (i + 100);
                }
            }

            return hash;
        }

        private static int HashFloat(float f)
        {
            return Mathf.RoundToInt(f * 10000f);
        }

        // ─── Vector Helpers ───────────────────────────────────

        private static void FillDeterministic(float[] vector, int start, int end, int seed)
        {
            for (int i = start; i < end; i++)
            {
                vector[i] = DeterministicRandom(seed + i);
            }
        }

        private static void FillRandom(float[] vector, int start, int end, float variance)
        {
            for (int i = start; i < end; i++)
            {
                vector[i] = DeterministicRandom(i * 7919) * variance
                           + UnityEngine.Random.Range(-variance, variance);
            }
        }

        /// <summary>
        /// Deterministic pseudo-random using integer hash.
        /// Returns value in [-1, 1].
        /// </summary>
        private static float DeterministicRandom(int seed)
        {
            // Robert Jenkins' 32-bit integer hash
            seed = ((seed >> 16) ^ seed) * 0x45d9f3b;
            seed = ((seed >> 16) ^ seed) * 0x45d9f3b;
            seed = (seed >> 16) ^ seed;
            return (seed & 0x7FFFFFFF) / (float)0x7FFFFFFF * 2f - 1f;
        }

        private static void Normalize(float[] vector)
        {
            float magnitude = 0f;
            for (int i = 0; i < vector.Length; i++)
                magnitude += vector[i] * vector[i];

            magnitude = Mathf.Sqrt(magnitude);
            if (magnitude < 0.0001f) return;

            for (int i = 0; i < vector.Length; i++)
                vector[i] /= magnitude;
        }

        // ─── Serialization ────────────────────────────────────

        public BatchSignatureSaveData GetSaveData()
        {
            return new BatchSignatureSaveData
            {
                batchId = _batchId,
                vector = (float[])_vector.Clone(),
                facilitySeed = _facilitySeed,
                recipeHash = _recipeHash,
                operatorHash = _operatorHash,
                creationTime = _creationTime,
                creationDay = _creationDay,
                scrubLevel = _scrubLevel,
                productId = _productId,
                quality = _quality
            };
        }

        public static BatchSignature FromSaveData(BatchSignatureSaveData data)
        {
            var sig = new BatchSignature
            {
                _batchId = data.batchId,
                _vector = data.vector ?? new float[VECTOR_SIZE],
                _facilitySeed = data.facilitySeed,
                _recipeHash = data.recipeHash,
                _operatorHash = data.operatorHash,
                _creationTime = data.creationTime,
                _creationDay = data.creationDay,
                _scrubLevel = data.scrubLevel,
                _productId = data.productId,
                _quality = data.quality
            };
            return sig;
        }
    }

    // ─── Save Data ────────────────────────────────────────────

    [Serializable]
    public struct BatchSignatureSaveData
    {
        public string batchId;
        public float[] vector;
        public int facilitySeed;
        public int recipeHash;
        public int operatorHash;
        public float creationTime;
        public int creationDay;
        public int scrubLevel;
        public string productId;
        public float quality;
    }
}
