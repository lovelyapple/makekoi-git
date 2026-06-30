using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Makekoi.Domain.Interfaces
{
	public interface IKoiRepository
	{
		int GetFoodStock();
	}
}

namespace Makekoi.UseCases
{
	using Makekoi.Domain.Interfaces;

	public sealed class FeedKoiUseCase
	{
		private readonly IKoiRepository _koiRepository;

		public FeedKoiUseCase(IKoiRepository koiRepository)
		{
			_koiRepository = koiRepository;
		}

		public bool CanFeed()
		{
			return _koiRepository.GetFoodStock() > 0;
		}
	}
}

namespace Makekoi.Infrastructure.Repositories
{
	using Makekoi.Domain.Interfaces;

	public sealed class KoiRepository : IKoiRepository
	{
		public int GetFoodStock()
		{
			return 10;
		}
	}
}

namespace Makekoi.Presentation.Presenters
{
	using Makekoi.UseCases;

	public sealed class FeedKoiStartupPresenter : IStartable
	{
		private readonly FeedKoiUseCase _feedKoiUseCase;

		public FeedKoiStartupPresenter(FeedKoiUseCase feedKoiUseCase)
		{
			_feedKoiUseCase = feedKoiUseCase;
		}

		public void Start()
		{
			Debug.Log($"[VContainer] FeedKoiUseCase initialized. CanFeed={_feedKoiUseCase.CanFeed()}");
		}
	}
}

namespace Makekoi.DI
{
	using Makekoi.Domain.Interfaces;
	using Makekoi.Infrastructure.Repositories;
	using Makekoi.Presentation.Presenters;
	using Makekoi.UseCases;

	public sealed class GameLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			builder.Register<IKoiRepository, KoiRepository>(Lifetime.Singleton);
			builder.Register<FeedKoiUseCase>(Lifetime.Transient);
			builder.RegisterEntryPoint<FeedKoiStartupPresenter>();
		}
	}

	public static class VContainerBootstrap
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void EnsureLifetimeScope()
		{
			if (UnityEngine.Object.FindObjectOfType<GameLifetimeScope>() != null)
			{
				return;
			}

			var root = new GameObject("[DI] GameLifetimeScope");
			root.AddComponent<GameLifetimeScope>();
			UnityEngine.Object.DontDestroyOnLoad(root);
		}
	}
}
