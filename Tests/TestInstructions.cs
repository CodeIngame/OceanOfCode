using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    public class TestInstructions
    {

        public List<string> Seeds { get; set; } = new List<string> { "MOVE N TORPEDO", "MOVE E| TORPEDO 8 6", "MOVE E| TORPEDO 8 6", "TORPEDO 0 8|MOVE E TORPEDO", "SURFACE 5 | MOVE W", "SURFACE 7" };


        public void Handle() {

            foreach(var s in Seeds)
            {
                var cmd = s;
                var isMultiOrders = cmd.Contains("|");

                foreach(var order in cmd.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {

                }

              
            }


        }
    }
}
