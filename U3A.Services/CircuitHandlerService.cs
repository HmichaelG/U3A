using DevExpress.XtraRichEdit.Model;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using U3A.Database;

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
            IHttpContextAccessor accessor = new HttpContextAccessor();
            var id = accessor.HttpContext.User.Identity; // store in dictionary somewhere
            var cd = new CircuitDetail()
            {
                Id = circuit.Id,
                Name = (!string.IsNullOrWhiteSpace(id?.Name)) ? id.Name : "Anonymous (Public)",
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
            if (kvp!.Value != null) { kvp.Value.Down = DateTime.Now; }
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
        public DateTime? Created { get; set; } = DateTime.Now;
        public DateTime? Down { get; set; }

        public string UpTime
        {
            get
            {
                return ((TimeSpan)(DateTime.Now - Created!)).ToString(@"hh\:mm\:ss");
            }
        }
        public string DownTime
        {
            get
            {
                return (Down != null)
                        ? "Down: " + ((TimeSpan)(DateTime.Now - Down!)).ToString(@"hh\:mm\:ss")
                        : "Active";
            }
        }
    }
}