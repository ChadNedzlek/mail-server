using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Reflection;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpImplementationFactory : Factory<ICommandFactory>
	{
		private static readonly Lazy<SmtpImplementationFactory> DefaultLazy =
			new Lazy<SmtpImplementationFactory>(CreateDefault);

		private static SmtpImplementationFactory CreateDefault()
		{
			var factory = new SmtpImplementationFactory();
			factory.ImportDefault();
			return factory;
		}

		public new static SmtpImplementationFactory Default => DefaultLazy.Value;

		protected override IEnumerable<Assembly> GetDefaultAssemblies()
		{
			return new[]
			{
				typeof (SmtpImplementationFactory).GetTypeInfo().Assembly,
				typeof (IAuthenticationMechanism).GetTypeInfo().Assembly,
			};
		}

		protected override void OnImport(CompositionHost host)
		{
			base.OnImport(host);
			Authentication.Import(host);
		}

		public Factory<IAuthenticationMechanism> Authentication { get; } = new Factory<IAuthenticationMechanism>();
	}
}