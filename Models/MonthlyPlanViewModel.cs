using System;
using System.Collections.Generic;

namespace Etutlist.Models
{
    public class MonthlyPlanViewModel
    {
        public Dictionary<DateTime, List<Etut>> Plan { get; set; } = new();
        public List<Personel> Yedekler { get; set; } = new();
        public int Yil { get; set; }
        public int Ay { get; set; }
    }
}