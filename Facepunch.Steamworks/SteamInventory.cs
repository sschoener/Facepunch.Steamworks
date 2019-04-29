﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;

namespace Steamworks
{
	/// <summary>
	/// Undocumented Parental Settings
	/// </summary>
	public static class SteamInventory
	{
		static ISteamInventory _internal;
		internal static ISteamInventory Internal
		{
			get
			{
				if ( _internal == null )
					_internal = new ISteamInventory();

				return _internal;
			}
		}

		internal static void InstallEvents()
		{
			new Event<SteamInventoryFullUpdate_t>( x => OnInventoryUpdated?.Invoke() );
			new Event<SteamInventoryDefinitionUpdate_t>( x => DefinitionsUpdated() );
		}

		public static event Action OnInventoryUpdated;
		public static event Action OnDefinitionsUpdated;

		internal static int defUpdateCount = 0;

		internal static void DefinitionsUpdated()
		{
			Definitions = GetDefinitions();

			if ( Definitions != null )
			{
				_defMap = new Dictionary<int, InventoryDef>();

				foreach ( var d in Definitions )
				{
					_defMap[d.Id] = d;
				}
			}

			defUpdateCount++;

			OnDefinitionsUpdated?.Invoke();
		}


		/// <summary>
		/// Call this if you're going to want to access definition information. You should be able to get 
		/// away with calling this once at the start if your game, assuming your items don't change all the time.
		/// This will trigger OnDefinitionsUpdated at which point Definitions should be set.
		/// </summary>
		public static void LoadItemDefinitions()
		{
			Internal.LoadItemDefinitions();
		}

		/// <summary>
		/// Will call LoadItemDefinitions and wait until Definitions is not null
		/// </summary>
		public static async Task<bool> WaitForDefinitions( float timeoutSeconds = 10 )
		{
			LoadItemDefinitions();

			var sw = Stopwatch.StartNew();

			while ( Definitions == null )
			{
				if ( sw.Elapsed.TotalSeconds > timeoutSeconds )
					return false;

				await Task.Delay( 10 );
			}

			return true;
		}

		internal static InventoryDef FindDefinition( InventoryDefId defId )
		{
			if ( _defMap.TryGetValue( defId, out var val  ) )
				return val;

			return null;
		}

		public static string Currency { get; internal set; }

		public static async Task<InventoryDef[]> GetDefinitionsWithPricesAsync()
		{
			var priceRequest = await Internal.RequestPrices();
			if ( !priceRequest.HasValue || priceRequest.Value.Result != Result.OK )
				return null;

			Currency = priceRequest?.Currency;

			var num = Internal.GetNumItemsWithPrices();

			if ( num <= 0 )
				return null;

			var defs = new InventoryDefId[num];
			var currentPrices = new ulong[num];
			var baseprices = new ulong[num];

			var gotPrices = Internal.GetItemsWithPrices( defs, currentPrices, baseprices, num );
			if ( !gotPrices )
				return null;

			return defs.Select( x => new InventoryDef( x ) ).ToArray();
		}

		public static InventoryDef[] Definitions { get; internal set; }
		public static Dictionary<int, InventoryDef> _defMap;

		internal static InventoryDef[] GetDefinitions()
		{
			uint num = 0;
			if ( !Internal.GetItemDefinitionIDs( null, ref num ) )
				return null;

			var defs = new InventoryDefId[num];

			if ( !Internal.GetItemDefinitionIDs( defs, ref num ) )
				return null;

			return defs.Select( x => new InventoryDef( x ) ).ToArray();
		}

		public static async Task<InventoryResult?> GetItems()
		{
			var sresult = default( SteamInventoryResult_t );

			if ( !Internal.GetAllItems( ref sresult ) )
				return null;

			return await InventoryResult.GetAsync( sresult );
		}

	}
}