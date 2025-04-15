using System.Diagnostics.Metrics;

namespace WfcWebApp.Utils
{

public static class RandomUtils
{
    public static Random Random = new();

    public static void Reseed(int seed) {
        Random = new Random(seed);
    }

    public static T Choice<T>(IList<T> arrayLike) {
        return arrayLike[Random.Next(arrayLike.Count)];
    }


    private static T GetWeightedRandomKey<T>(Dictionary<T, int> counter)
	{
		if (counter == null || counter.Count == 0)
			throw new InvalidOperationException("The dictionary is empty or null.");

		// Step 1: Calculate the total number of "tickets"
		int totalWeight = 0;
		foreach (var entry in counter)
		{
			if (entry.Value == 0) {
				throw new InvalidOperationException("Entry value is 0.");
			}
			totalWeight += entry.Value;
		}

		// Step 2: Generate a random number between 1 and totalWeight
		int randomTicket = Random.Next(1, totalWeight + 1); // Random number in range [1, totalWeight]

		// Step 3: Traverse the dictionary to find the corresponding key
		int cumulativeWeight = 0;
		foreach (var entry in counter)
		{
			cumulativeWeight += entry.Value;
			if (randomTicket <= cumulativeWeight)
			{
				return entry.Key; // Return the key where the random ticket falls
			}
		}

		// In case something goes wrong (this should never happen if the logic is correct)
		throw new InvalidOperationException("Failed to select a key.");
	}

	private static T GetHeaviestKey<T>(Dictionary<T, int> counter)
	{
		T heaviestKey = counter.GetEnumerator().Current.Key; //TODO fix this jank
		int heaviestValue = -1;
		foreach (var entry in counter)
		{
			if (entry.Value > heaviestValue) {
				heaviestKey = entry.Key;
				heaviestValue = entry.Value;
			}
		}
		return heaviestKey;
	}

    public class WeightedKeyCounter<T> {
        private Dictionary<T, int> counter = new();

        public void AddWeightedKey(T key, int weight) {
            if (counter.TryGetValue(key, out int value)) {
                counter[key] = value + weight;
            } else {
                counter[key] = weight;
            }
        }

        public void Clear() {
            counter.Clear();
        }

        public int Count() {
            return counter.Count;
        }

        public T Sample() {
            return GetWeightedRandomKey(counter);
        }
    }

}




}