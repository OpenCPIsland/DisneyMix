using System;
using System.Collections.Generic;
using System.Linq;
using Mix.Games.Tray.ObjectPool;
using UnityEngine;

namespace Mix.Games.Tray.Drop
{
	public class LevelGenerator : MonoBehaviour
	{
		public DropGame Game;

		[Tooltip("Approximate time it takes the player to jump between platforms.")]
		[Space(10f)]
		public float ApproximateJumpTime = 0.26f;

		public AnimationCurve GameTimeMultiplerForPatternNumber;

		[Tooltip("Amount of time in seconds to generate ahead")]
		[Space(10f)]
		public float MinLeadTime;

		public string InitialPatternName;

		[Space(10f)]
		public int DefaultPatternWeight = 100;

		[Space(10f)]
		public int PlatformPoolSize;

		public int PatternPoolSize;

		private int patternNumber;

		private Pattern currentPattern;

		private List<Pattern> oldPatterns = new List<Pattern>();

		private List<Pattern> nextPatterns = new List<Pattern>();

		public List<PatternTemplate> BasePatternTemplates { get; set; }

		public List<PatternTemplate> patternTemplates { get; private set; }

		private List<PatternTemplate> unsortedTemplates { get; set; }

		public System.Random SeededRandomGenerator { get; private set; }

		public int RandomSeed { get; private set; }

		public string CurrentPatternName
		{
			get
			{
				return (!currentPattern) ? string.Empty : currentPattern.TemplateName;
			}
		}

		private void OnEnable()
		{
			DropGame game = Game;
			game.OnGameTimeUpdated = (Action<float>)Delegate.Combine(game.OnGameTimeUpdated, new Action<float>(OnGameTimeUpdate));
		}

		private void OnDisable()
		{
			DropGame game = Game;
			game.OnGameTimeUpdated = (Action<float>)Delegate.Remove(game.OnGameTimeUpdated, new Action<float>(OnGameTimeUpdate));
		}

		private void Awake()
		{
			BasePatternTemplates = new List<PatternTemplate>();
			if (!ObjectPoolManager.HasPoolForObj(Game.PatternPrefab.gameObject))
			{
				ObjectPoolManager.CreatePool(Game.PatternPrefab.gameObject, PatternPoolSize);
			}
			if (!ObjectPoolManager.HasPoolForObj(Game.PlatformPrefab.gameObject))
			{
				ObjectPoolManager.CreatePool(Game.PlatformPrefab.gameObject, PlatformPoolSize);
			}
		}

		public void CreatePatterns(List<PatternData> patternDatas)
		{
			if (patternTemplates == null)
			{
				patternTemplates = new List<PatternTemplate>();
			}
			List<PatternTemplate> newTemplates = new List<PatternTemplate>();
			for (int i = 0; i < patternDatas.Count; i++)
			{
				newTemplates.Clear();
				PatternTemplate patternTemplate = new PatternTemplate(patternDatas[i]);
				if (patternTemplate.Weight < 0)
				{
					patternTemplate.Weight = DefaultPatternWeight;
				}
				newTemplates.Add(patternTemplate);
				newTemplates.AddRange(CreateReflectedPatterns(patternTemplate));
				if (patternTemplate.CanRotate90)
				{
					PatternTemplate patternTemplate2 = PatternTemplate.CreateRotatedPattern(patternTemplate);
					newTemplates.Add(patternTemplate2);
					newTemplates.AddRange(CreateReflectedPatterns(patternTemplate2));
				}
				newTemplates.ForEach(delegate(PatternTemplate x)
				{
					x.Weight /= newTemplates.Count;
				});
				patternTemplates.AddRange(newTemplates);
			}
			unsortedTemplates = patternTemplates.Select((PatternTemplate item) => (PatternTemplate)item.Clone()).ToList();
		}

		private int GetTotalPatternWeight(int currentPatternNumber)
		{
			int num = 0;
			for (int i = 0; i < patternTemplates.Count; i++)
			{
				num += patternTemplates[i].GetWeightForPatternNumber(currentPatternNumber);
			}
			return num;
		}

		private List<PatternTemplate> CreateReflectedPatterns(PatternTemplate template)
		{
			List<PatternTemplate> list = new List<PatternTemplate>();
			if (template.CanReflectX)
			{
				list.Add(PatternTemplate.CreateReflectedPattern(template, true, false));
			}
			if (template.CanReflectY)
			{
				list.Add(PatternTemplate.CreateReflectedPattern(template, false, true));
			}
			if (template.CanReflectX && template.CanReflectY)
			{
				list.Add(PatternTemplate.CreateReflectedPattern(template, true, true));
			}
			return list;
		}

		public float GetGameTimeMultiplierForPattern(int patternNumber)
		{
			if (patternNumber <= 0)
			{
				return 1f;
			}
			return ApproximateJumpTime * GameTimeMultiplerForPatternNumber.Evaluate(patternNumber);
		}

		private PatternTemplate GetPatternByName(string aName)
		{
			return patternTemplates.FirstOrDefault((PatternTemplate pattern) => pattern.Name == aName);
		}

		public void SetRandomSeed(int seed)
		{
			RandomSeed = seed;
			SeededRandomGenerator = new System.Random(seed);
		}

		public int GetSeededRandomRange(int min, int max)
		{
			return SeededRandomGenerator.Next(min, max);
		}

		public float GetSeededRandomRange(float min, float max)
		{
			return Mathf.Lerp(min, max, (float)SeededRandomGenerator.NextDouble());
		}

		public float GetSeededRandom()
		{
			return (float)SeededRandomGenerator.NextDouble();
		}

		public void Initialize(int seed)
		{
			SetRandomSeed(seed);
			patternTemplates = unsortedTemplates.Select((PatternTemplate item) => (PatternTemplate)item.Clone()).ToList();
			patternTemplates.Shuffle(SeededRandomGenerator);
		}

		public void Reset()
		{
			UnspawnPattern(currentPattern);
			currentPattern = null;
			for (int i = 0; i < oldPatterns.Count; i++)
			{
				UnspawnPattern(oldPatterns[i]);
			}
			oldPatterns.Clear();
			for (int j = 0; j < nextPatterns.Count; j++)
			{
				UnspawnPattern(nextPatterns[j]);
			}
			nextPatterns.Clear();
			patternNumber = 0;
			Awake();
			Initialize(RandomSeed);
		}

		public Pattern GenerateInitialPattern()
		{
			PatternTemplate patternByName = GetPatternByName(InitialPatternName);
			Pattern pattern = CreatePatternFromTemplate(patternByName, new Coordinate2D(0, 0), 0f, 0);
			pattern.StartPlatform.IsFirstPlatform = true;
			pattern.transform.SetParent(base.transform, false);
			currentPattern = pattern;
			patternNumber = 1;
			while (pattern == null || pattern.EndTime < MinLeadTime)
			{
				Pattern pattern2 = CreatePattern(pattern.EndPlatform, pattern.EndTime, patternNumber + nextPatterns.Count);
				nextPatterns.Add(pattern2);
				pattern = pattern2;
			}
			return currentPattern;
		}

		private bool IsColumnAtGridCoordinate(Coordinate2D coords)
		{
			return Game.ColumnGenerator != null && Game.ColumnGenerator.IsColumnAtGridCoordinate(coords);
		}

		public bool IsGridCoordinateOccupied(Coordinate2D coords, bool useAutoFill = true)
		{
			if (currentPattern.IsCoordinateUsedInPattern(coords))
			{
				return true;
			}
			if (IsColumnAtGridCoordinate(coords))
			{
				return true;
			}
			for (int i = 0; i < oldPatterns.Count; i++)
			{
				if (oldPatterns[i].IsCoordinateUsedInPattern(coords))
				{
					return true;
				}
			}
			for (int j = 0; j < nextPatterns.Count; j++)
			{
				if (nextPatterns[j].IsCoordinateUsedInPattern(coords))
				{
					return true;
				}
			}
			if (useAutoFill && GetOccupiedNeighbors(coords) >= 3)
			{
				return true;
			}
			return false;
		}

		private int GetOccupiedNeighbors(Coordinate2D coords)
		{
			int num = 0;
			if (IsGridCoordinateOccupied(new Coordinate2D(coords.x + 1, coords.y), false))
			{
				num++;
			}
			if (IsGridCoordinateOccupied(new Coordinate2D(coords.x - 1, coords.y), false))
			{
				num++;
			}
			if (IsGridCoordinateOccupied(new Coordinate2D(coords.x, coords.y + 1), false))
			{
				num++;
			}
			if (IsGridCoordinateOccupied(new Coordinate2D(coords.x, coords.y - 1), false))
			{
				num++;
			}
			return num;
		}

		public Platform GetPlatformAtCoords(Coordinate2D coords)
		{
			Platform platformAtCoords = currentPattern.GetPlatformAtCoords(coords);
			if (platformAtCoords != null)
			{
				return platformAtCoords;
			}
			for (int i = 0; i < nextPatterns.Count; i++)
			{
				platformAtCoords = nextPatterns[i].GetPlatformAtCoords(coords);
				if ((bool)platformAtCoords)
				{
					return platformAtCoords;
				}
			}
			for (int j = 0; j < oldPatterns.Count; j++)
			{
				platformAtCoords = oldPatterns[j].GetPlatformAtCoords(coords);
				if ((bool)platformAtCoords)
				{
					return platformAtCoords;
				}
			}
			return null;
		}

		public Vector3 GetPositionForCoords(Coordinate2D coords)
		{
			return base.transform.TransformPoint(new Vector3(Game.GridSpacing.x * (float)coords.x, 0f, Game.GridSpacing.y * (float)coords.y));
		}

		private void OnGameTimeUpdate(float gameTime)
		{
			if (!(currentPattern != null))
			{
				return;
			}
			if (gameTime > currentPattern.EndTime)
			{
				currentPattern.name += " - OLD";
				oldPatterns.Add(currentPattern);
				currentPattern = nextPatterns[0];
				patternNumber++;
				nextPatterns.RemoveAt(0);
			}
			Pattern pattern = ((nextPatterns.Count != 0) ? nextPatterns[nextPatterns.Count - 1] : currentPattern);
			if (pattern.EndTime < gameTime + MinLeadTime)
			{
				Pattern pattern2 = CreatePattern(pattern.EndPlatform, pattern.EndTime, patternNumber + nextPatterns.Count);
				if (pattern2 != null)
				{
					nextPatterns.Add(pattern2);
				}
				else
				{
					pattern.EndPlatform.Configuration.ExitTime += 0.2f;
					pattern.EndTime += 0.2f;
				}
			}
			if (oldPatterns.Count > 0 && oldPatterns[0].IsFinishedAtTime(gameTime))
			{
				UnspawnPattern(oldPatterns[0]);
				oldPatterns.RemoveAt(0);
			}
		}

		private Pattern CreatePattern(Platform connectingPlatform, float time, int nextPatternNumber)
		{
			Pattern pattern = null;
			int num = SeededRandomGenerator.Next(GetTotalPatternWeight(nextPatternNumber));
			int num2 = 0;
			bool flag = false;
			while (!flag && num2 < patternTemplates.Count)
			{
				num -= patternTemplates[num2].GetWeightForPatternNumber(nextPatternNumber);
				if (num > 0)
				{
					num2++;
				}
				else
				{
					flag = true;
				}
			}
			for (int i = 0; i < patternTemplates.Count; i++)
			{
				if (!(pattern == null))
				{
					break;
				}
				int index = (num2 + i) % patternTemplates.Count;
				PatternTemplate patternTemplate = patternTemplates[index];
				if (patternTemplate.GetWeightForPatternNumber(nextPatternNumber) > 0 && PatternCanFitOnGrid(patternTemplate, connectingPlatform.Configuration.GridCoordinates))
				{
					pattern = CreatePatternFromTemplate(patternTemplate, connectingPlatform.Configuration.GridCoordinates, time, nextPatternNumber);
					if (pattern != null)
					{
						pattern.name = nextPatternNumber + ": " + pattern.name;
						pattern.transform.SetParent(base.transform, false);
						connectingPlatform.Configuration.NextPlatform = pattern.Path[0];
					}
				}
			}
			patternTemplates.Shuffle(SeededRandomGenerator);
			return pattern;
		}

		private bool PatternCanFitOnGrid(PatternTemplate patternTemplate, Coordinate2D origin)
		{
			List<Coordinate2D> footprint = patternTemplate.GetFootprint(origin);
			for (int i = 0; i < footprint.Count; i++)
			{
				if (IsGridCoordinateOccupied(footprint[i]))
				{
					return false;
				}
			}
			return true;
		}

		private Platform SpawnPlatform()
		{
			return ObjectPoolManager.InstantiatePooledObj(Game.PlatformPrefab.gameObject).GetComponent<Platform>();
		}

		private void UnspawnPlatform(Platform platform)
		{
			ObjectPoolManager.DestroyPooledObj(platform.gameObject);
		}

		private void UnspawnPattern(Pattern pattern)
		{
			if (pattern != null)
			{
				for (int i = 0; i < pattern.Platforms.Count; i++)
				{
					UnspawnPlatform(pattern.Platforms[i]);
				}
				ObjectPoolManager.DestroyPooledObj(pattern.gameObject);
			}
		}

		private Pattern SpawnPattern()
		{
			return ObjectPoolManager.InstantiatePooledObj(Game.PatternPrefab.gameObject).GetComponent<Pattern>();
		}

		private Pattern CreatePatternFromTemplate(PatternTemplate template, Coordinate2D origin, float time, int generatedPatternNumber = -1)
		{
			Pattern pattern = SpawnPattern();
			Debug.Log("template null=" + (template == null) + ", pattern null=" + (pattern == null)
    + ", InitialPatternName='" + InitialPatternName + "'"
    + ", templates=" + (patternTemplates == null ? -1 : patternTemplates.Count));
			pattern.name = template.Name;
			pattern.TemplateName = template.Name;
			if (generatedPatternNumber == -1)
			{
				generatedPatternNumber = patternNumber;
			}
			float gameTimeMultiplierForPattern = GetGameTimeMultiplierForPattern(generatedPatternNumber);
			time -= template.Overlap * gameTimeMultiplierForPattern;
			for (int i = 0; i < template.PathPlatforms.Count; i++)
			{
				Platform platform = SpawnPlatform();
				platform.CopyConfiguration(template.PathPlatforms[i], origin, time, gameTimeMultiplierForPattern);
				platform.PatternNumber = generatedPatternNumber;
				platform.name = pattern.name + " - Platform " + i + " " + platform.Configuration.GridCoordinates;
				pattern.AddPlatform(platform, true);
				if (i > 0)
				{
					pattern.Path[i - 1].Configuration.NextPlatform = platform;
				}
			}
			for (int j = 0; j < template.DecoyPlatforms.Count; j++)
			{
				Platform platform2 = SpawnPlatform();
				platform2.name = pattern.name + " - Decoy " + j;
				platform2.CopyConfiguration(template.DecoyPlatforms[j], origin, time, gameTimeMultiplierForPattern);
				platform2.PatternNumber = generatedPatternNumber;
				if (template.DecoyPlatforms[j].PathOrder >= 0)
				{
					platform2.Configuration.NextPlatform = pattern.Path[template.DecoyPlatforms[j].PathOrder];
				}
				else
				{
					platform2.Configuration.NextPlatform = GetPlatformAtCoords(origin);
				}
				pattern.AddPlatform(platform2);
			}
			pattern.EndTime = time + template.NextPatternTime * gameTimeMultiplierForPattern;
			return pattern;
		}
	}
}
