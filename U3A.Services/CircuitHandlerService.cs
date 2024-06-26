﻿using DevExpress.XtraPrinting.Native;
using DevExpress.XtraRichEdit.Model;
using Microsoft.AspNetCore.Components.Server.Circuits;
using ASP_CORE = Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using U3A.Database;
using Microsoft.AspNetCore.Http;
using U3A.Model;

namespace U3A.Services
{
    public class CircuitHandlerService : CircuitHandler
    {
        public ConcurrentDictionary<string, CircuitDetail> CircuitDetails
        {
            get;
            set;
        }
        public event EventHandler CircuitsChanged;

        protected virtual void OnCircuitsChanged()
             => CircuitsChanged?.Invoke(this, EventArgs.Empty);

        public CircuitHandlerService()
        {
            CircuitDetails = new ConcurrentDictionary<string, CircuitDetail>();
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit,
                             CancellationToken cancellationToken)
        {
            IHttpContextAccessor accessor = new ASP_CORE.HttpContextAccessor();
            if (accessor == null) { return base.OnCircuitOpenedAsync(circuit, cancellationToken); }
            if (accessor.HttpContext == null) { return base.OnCircuitOpenedAsync(circuit, cancellationToken); }
            if (accessor.HttpContext.User == null) { return base.OnCircuitOpenedAsync(circuit, cancellationToken); }
            if (accessor.HttpContext.User.Identity == null) { return base.OnCircuitOpenedAsync(circuit, cancellationToken); }
            if (accessor.HttpContext.Request == null) { return base.OnCircuitOpenedAsync(circuit, cancellationToken); }
            if (accessor.HttpContext.Request.Host == null) { return base.OnCircuitOpenedAsync(circuit, cancellationToken); }
            if (accessor.HttpContext.Request.Host.Host == null) { return base.OnCircuitOpenedAsync(circuit, cancellationToken); }
            var id = accessor.HttpContext.User.Identity;
            HostStrategy hs = new HostStrategy();
            var tenant = hs.GetIdentifier(accessor.HttpContext.Request.Host.Host);
            var cd = new CircuitDetail()
            {
                Id = circuit.Id,
                Name = (!string.IsNullOrWhiteSpace(id?.Name)) ? id.Name : "Anonymous (Public)",
                Tenant = tenant,
            };
            CircuitDetails.TryAdd(circuit.Id, cd);
            OnCircuitsChanged();
            return base.OnCircuitOpenedAsync(circuit,
                                  cancellationToken);
        }

        public override Task OnCircuitClosedAsync(Circuit circuit,
                  CancellationToken cancellationToken)
        {
            CircuitDetail circuitRemoved;
            CircuitDetails.TryRemove(circuit.Id, out circuitRemoved);
            OnCircuitsChanged();           
            return base.OnCircuitClosedAsync(circuit,
                              cancellationToken);
        }

        public override Task OnConnectionDownAsync(Circuit circuit,
                              CancellationToken cancellationToken)
        {
            var kvp = CircuitDetails.FirstOrDefault(x => x.Value.Id == circuit.Id);
            if (kvp!.Value != null) { kvp.Value.Down = DateTime.UtcNow; }
            return base.OnConnectionDownAsync(circuit,
                             cancellationToken);
        }

        public override Task OnConnectionUpAsync(Circuit circuit,
                            CancellationToken cancellationToken)
        {
            var kvp = CircuitDetails.FirstOrDefault(x => x.Value.Id == circuit.Id);
            if (kvp!.Value != null) { kvp.Value.Down = null; }
            return base.OnConnectionUpAsync(circuit, cancellationToken);
        }
    }

    public class CircuitDetail
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Tenant { get; set; }
        public DateTime? Created { get; set; } = DateTime.UtcNow;
        public DateTime? Down { get; set; }

        public string UpTime
        {
            get
            {
                return ((TimeSpan)(DateTime.UtcNow - Created!)).ToString(@"hh\:mm\:ss");
            }
        }
        public string DownTime
        {
            get
            {
                return (Down != null)
                        ? "Down: " + ((TimeSpan)(DateTime.UtcNow - Down!)).ToString(@"hh\:mm\:ss")
                        : "Active";
            }
        }
    }
}