﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class Schedule : BaseEntity
    {
        public Guid ID { get; set; }
        public string jsonClass { get; set; }
        public string jsonClassEnrolments { get; set; }
        public string jsonCourseEnrolments { get; set;}
    }
}