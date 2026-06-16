namespace Diplom.Models.Fields
{
    public class GeoJsonGeometry
    {
        public string type { get; set; }
        public List<List<double[]>> coordinates { get; set; }
    }
}
