#if UNITY_2018_1_OR_NEWER && (DEVELOPMENT_BUILD || UNITY_EDITOR)
#define ENABLE_GFX_MEMORY
#endif

namespace CodeStage.AdvancedFPSCounter.CountersData
{
	using System.Collections;
	using UnityEngine;

	using Utils;

#if UNITY_5_5_OR_NEWER
	using UnityEngine.Profiling;
	#endif

	/// <summary>
	/// Shows memory usage data.
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	public class MemoryCounterData: UpdatableCounterData
	{
		// ----------------------------------------------------------------------------
		// constants
		// ----------------------------------------------------------------------------

		public const long MemoryDivider = 1048576; // 1024^2

		private const string TextStart = "<color=#{0}>";
		private const string LineStartTotal = "MEM TOTAL: ";
		private const string LineStartAllocated = "MEM ALLOC: ";
		private const string LineStartMono = "MEM MONO: ";
#if ENABLE_GFX_MEMORY
		private const string LineStartGfx = "MEM GFX: ";
#endif
		private const string LineEnd = " MB";
		private const string TextEnd = "</color>";

		// ----------------------------------------------------------------------------
		// properties exposed to the inspector
		// ----------------------------------------------------------------------------

#region Precise
		[Tooltip("Allows to output memory usage more precisely thus using a bit more system resources.")]
		[SerializeField]
		private bool precise = true;

		/// <summary>
		/// Allows to output memory usage more precisely thus using a bit more system resources.
		/// </summary>
		public bool Precise
		{
			get { return precise; }
			set
			{
				if (precise == value || !Application.isPlaying) return;
				precise = value;
				if (!enabled) return;

				Refresh();
			}
		}
#endregion

#region Total
		[Tooltip("Allows to see private memory amount reserved for application. This memory can’t be used by other applications.")]
		[SerializeField]
		private bool total = true;

		/// <summary>
		/// Allows to see private memory amount reserved for application. This memory can’t be used by other applications.
		/// </summary>
		public bool Total
		{
			get { return total; }
			set
			{
				if (total == value || !Application.isPlaying) return;
				total = value;
				if (!total) LastTotalValue = 0;
				if (!enabled) return;

				Refresh();
			}
		}
#endregion

#region Allocated
		[Tooltip("Allows to see amount of memory, currently allocated by application.")]
		[SerializeField]
		private bool allocated = true;

		/// <summary>
		/// Allows to see amount of memory, currently allocated by application.
		/// </summary>
		public bool Allocated
		{
			get { return allocated; }
			set
			{
				if (allocated == value || !Application.isPlaying) return;
				allocated = value;
				if (!allocated) LastAllocatedValue = 0;
				if (!enabled) return;

				Refresh();
			}
		}
#endregion

#region MonoUsage
		[Tooltip("Allows to see amount of memory, allocated by managed Mono objects, " +
		         "such as UnityEngine.Object and everything derived from it for example.")]
		[SerializeField]
		private bool monoUsage;

		/// <summary>
		/// Allows to see amount of memory, allocated by managed Mono objects, 
		/// such as UnityEngine.Object and everything derived from it for example.
		/// </summary>
		public bool MonoUsage
		{
			get { return monoUsage; }
			set
			{
				if (monoUsage == value || !Application.isPlaying) return;
				monoUsage = value;
				if (!monoUsage) LastMonoValue = 0;
				if (!enabled) return;

				Refresh();
			}
		}
#endregion

#region GFX
		[Tooltip("Allows to see amount of allocated memory for the graphics driver (dev builds only).")]
		[SerializeField]
		private bool gfx = true;

		/// <summary>
		/// Allows to see amount of allocated memory for the graphics driver.
		/// Requires Unity 2018.1.0 or newer and Development build.
		/// This value is not included into the Total memory counter.
		/// </summary>
		public bool Gfx
		{
			get { return gfx; }
			set
			{
				if (gfx == value || !Application.isPlaying) return;
				gfx = value;
				if (!gfx) LastGfxValue = 0;
				if (!enabled) return;

				Refresh();
			}
		}
#endregion

		// ----------------------------------------------------------------------------
		// properties only accessible from code
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Last total memory readout.
		/// </summary>
		/// In megabytes if #Precise is false, in bytes otherwise.
		/// @see Total
		public long LastTotalValue { get; private set; }

		/// <summary>
		/// Last allocated memory readout.
		/// </summary>
		/// In megabytes if #Precise is false, in bytes otherwise.
		/// @see Allocated
		public long LastAllocatedValue { get; private set; }

		/// <summary>
		/// Last Mono memory readout.
		/// </summary>
		/// In megabytes if #Precise is false, in bytes otherwise.
		/// @see MonoUsage
		public long LastMonoValue { get; private set; }

		/// <summary>
		/// Last graphics driver memory readout.
		/// </summary>
		/// In megabytes if #Precise is false, in bytes otherwise.
		/// @see Gfx
		public long LastGfxValue { get; private set; }

		// ----------------------------------------------------------------------------
		// constructor
		// ----------------------------------------------------------------------------

		internal MemoryCounterData()
		{
			color = new Color32(234, 238, 101, 255);
			style = FontStyle.Bold;
		}

		// ----------------------------------------------------------------------------
		// internal methods
		// ----------------------------------------------------------------------------

		internal override void UpdateValue(bool force)
		{
			if (!enabled) return;

			if (force)
			{
				if (!inited && (HasData()))
				{
					Activate();
					return;
				}

				if (inited && (!HasData()))
				{
					Deactivate();
					return;
				}
			}

			if (total)
			{
#if UNITY_5_6_OR_NEWER
				var value = Profiler.GetTotalReservedMemoryLong();
#else
				var value = Profiler.GetTotalReservedMemory();
#endif
				long divisionResult = 0;

				bool newValue;
				if (precise)
				{
					newValue = LastTotalValue != value;
				}
				else
				{
					divisionResult = value / MemoryDivider;
					newValue = LastTotalValue != divisionResult;
				}

				if (newValue || force)
				{
					LastTotalValue = precise ? value : divisionResult;
					dirty = true;
				}
			}

			if (allocated)
			{
#if UNITY_5_6_OR_NEWER
				var value = Profiler.GetTotalAllocatedMemoryLong();
#else
				var value = Profiler.GetTotalAllocatedMemory();
#endif
				long divisionResult = 0;

				bool newValue;
				if (precise)
				{
					newValue = LastAllocatedValue != value;
				}
				else
				{
					divisionResult = value / MemoryDivider;
					newValue = (LastAllocatedValue != divisionResult);
				}

				if (newValue || force)
				{
					LastAllocatedValue = precise ? value : divisionResult;
					dirty = true;
				}
			}

			if (monoUsage)
			{
				var monoMemory = System.GC.GetTotalMemory(false);
				long divisionResult = 0;

				bool newValue;
				if (precise)
				{
					newValue = (LastMonoValue != monoMemory);
				}
				else
				{
					divisionResult = monoMemory / MemoryDivider;
					newValue = (LastMonoValue != divisionResult);
				}

				if (newValue || force)
				{
					LastMonoValue = precise ? monoMemory : divisionResult;
					dirty = true;
				}
			}

#if ENABLE_GFX_MEMORY
			if (gfx)
			{
				//var value = Profiler.GetAllocatedMemoryForGraphicsDriver();
				var value = SystemInfo.graphicsMemorySize;
				long divisionResult = 0;

				bool newValue;
				if (precise)
				{
					newValue = LastGfxValue != value;
				}
				else
				{
					divisionResult = value / MemoryDivider;
					newValue = LastGfxValue != divisionResult;
				}

				if (newValue || force)
				{
					LastGfxValue = precise ? value : divisionResult;
					dirty = true;
				}
			}
#endif

			if (!dirty || main.OperationMode != OperationMode.Normal) return;

			var needNewLine = false;

			text.Length = 0;
			text.Append(colorCached);

			if (total)
			{
				text.Append(LineStartTotal);

				if (precise)
				{
					text.AppendLookup(LastTotalValue / (float)MemoryDivider);
				}
				else
				{
					text.AppendLookup(LastTotalValue);
				}
				text.Append(LineEnd);
				needNewLine = true;
			}

			if (allocated)
			{
				if (needNewLine) text.Append(AFPSCounter.NewLine);
				text.Append(LineStartAllocated);

				if (precise)
				{
					text.AppendLookup(LastAllocatedValue / (float)MemoryDivider);
				}
				else
				{
					text.AppendLookup(LastAllocatedValue);
				}
				text.Append(LineEnd);
				needNewLine = true;
			}

			if (monoUsage)
			{
				if (needNewLine) text.Append(AFPSCounter.NewLine);
				text.Append(LineStartMono);

				if (precise)
				{
					text.AppendLookup(LastMonoValue / (float)MemoryDivider);
				}
				else
				{
					text.AppendLookup(LastMonoValue);
				}

				text.Append(LineEnd);
#if ENABLE_GFX_MEMORY
				needNewLine = true;
#endif
			}

#if ENABLE_GFX_MEMORY
			if (gfx)
			{
				if (needNewLine) text.Append(AFPSCounter.NewLine);
				text.Append(LineStartGfx);

				if (precise)
				{
					text.AppendLookup(LastGfxValue / (float)MemoryDivider);
				}
				else
				{
					text.AppendLookup(LastGfxValue);
				}

				text.Append(LineEnd);
			}
#endif

			text.Append(TextEnd);

			ApplyTextStyles();
		}

		// ----------------------------------------------------------------------------
		// protected methods
		// ----------------------------------------------------------------------------

		protected override void PerformActivationActions()
		{
			base.PerformActivationActions();

			if (!HasData()) return;

			LastTotalValue = 0;
			LastAllocatedValue = 0;
			LastMonoValue = 0;

			if (main.OperationMode == OperationMode.Normal)
			{
				if (colorCached == null)
				{
					colorCached = string.Format(TextStart, AFPSCounter.Color32ToHex(color));
				}

				text.Append(colorCached);

				if (total)
				{
					if (precise)
					{
						text.Append(LineStartTotal).Append("0.00").Append(LineEnd);
					}
					else
					{
						text.Append(LineStartTotal).Append(0).Append(LineEnd);
					}
				}

				if (allocated)
				{
					if (text.Length > 0) text.Append(AFPSCounter.NewLine);
					if (precise)
					{
						text.Append(LineStartAllocated).Append("0.00").Append(LineEnd);
					}
					else
					{
						text.Append(LineStartAllocated).Append(0).Append(LineEnd);
					}
				}

				if (monoUsage)
				{
					if (text.Length > 0) text.Append(AFPSCounter.NewLine);
					if (precise)
					{
						text.Append(LineStartMono).Append("0.00").Append(LineEnd);
					}
					else
					{
						text.Append(LineStartMono).Append(0).Append(LineEnd);
					}
				}

#if ENABLE_GFX_MEMORY
				if (gfx)
				{
					if (text.Length > 0) text.Append(AFPSCounter.NewLine);
					if (precise)
					{
						text.Append(LineStartGfx).Append("0.00").Append(LineEnd);
					}
					else
					{
						text.Append(LineStartGfx).Append(0).Append(LineEnd);
					}
				}
#endif

				text.Append(TextEnd);

				ApplyTextStyles();

				dirty = true;
			}
		}

		protected override void PerformDeActivationActions()
		{
			base.PerformDeActivationActions();

			if (text != null) text.Length = 0;

			main.MakeDrawableLabelDirty(anchor);
		}

		protected override IEnumerator UpdateCounter()
		{
			while (true)
			{
				UpdateValue();
				main.UpdateTexts();
				var previousUpdateTime = Time.unscaledTime;
				while (Time.unscaledTime < previousUpdateTime + updateInterval)
				{
					yield return null;
				}
			}
		}

		protected override bool HasData()
		{
			return total || allocated || monoUsage || gfx;
		}

		protected override void CacheCurrentColor()
		{
			colorCached = string.Format(TextStart, AFPSCounter.Color32ToHex(color));
		}
	}
}