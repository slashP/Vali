namespace Vali.Core;

public class Polygon(Polygon.PolyPoint[] Points) : ILatLng
{
    public record PolyPoint(double Lng, double Lat) : ILatLng;

    public class BoundingBox(
        double NLat,
        double ELng,
        double SLat,
        double WLng)
    {
        private PolyPoint? CenterPoint;
        private double LongestSide = double.NaN;

        public ILatLng GetCenterPoint()
        {
            if(CenterPoint != null)
            {
                return CenterPoint;
            }

            CenterPoint = new(
                (ELng - WLng) / 2 + WLng,
                (NLat - SLat) / 2 + SLat);
            return CenterPoint;
        }

        public double GetLongestSide()
        {
            if(!Double.IsNaN(LongestSide))
            {
                return LongestSide;
            }

            double longerLat = (Math.Abs(NLat) > Math.Abs(SLat)) ? SLat : NLat;
            double W = Extensions.ApproximateDistance(longerLat, ELng, longerLat, WLng);
            double H = Extensions.ApproximateDistance(NLat, WLng, SLat, WLng);
            LongestSide = Math.Max(W, H);
            return LongestSide;
        }

        public bool IsPointInBox(ILatLng point)
        {
            return point.Lat <= NLat && point.Lat >= SLat && point.Lng <= ELng && point.Lng >= WLng;
        }
    }

    public double Lat => GetCenterPoint().Lat;
    public double Lng => GetCenterPoint().Lng;

    private BoundingBox? Box;

    public BoundingBox GetBoundingBox()
    {
        if(Box != null)
        {
            return Box;
        }

        double NLat = double.NaN;
        double ELng = double.NaN;
        double SLat = double.NaN;
        double WLng = double.NaN;
        foreach (var p in Points)
        {
            if (p.Lat > NLat || Double.IsNaN(NLat)) NLat = p.Lat;
            if (p.Lng > ELng || Double.IsNaN(ELng)) ELng = p.Lng;
            if (p.Lat < SLat || Double.IsNaN(SLat)) SLat = p.Lat;
            if (p.Lng < WLng || Double.IsNaN(WLng)) WLng = p.Lng;
        }

        Box = new(NLat, ELng, SLat, WLng);
        return Box;
    }

    public ILatLng GetCenterPoint()
    {
        return GetBoundingBox().GetCenterPoint();
    }

    public bool IsPointInBoundingBox(ILatLng point)
    {
        return GetBoundingBox().IsPointInBox(point);
    }

    public bool IsPointInPolygon(ILatLng location)
    {
        bool result = false;
        int j = Points.Length - 1;
        for (int i = 0; i < Points.Length; i++)
        {
            if (Points[i].Lat < location.Lat && Points[j].Lat >= location.Lat ||
                Points[j].Lat < location.Lat && Points[i].Lat >= location.Lat)
            {
                if (Points[i].Lng + (location.Lat - Points[i].Lat) /
                   (Points[j].Lat - Points[i].Lat) *
                   (Points[j].Lng - Points[i].Lng) < location.Lng)
                {
                    result = !result;
                }
            }
            j = i;
        }
        return result;
    }

    public bool IsPointInside(ILatLng point)
    {
        return IsPointInBoundingBox(point) && IsPointInPolygon(point);
    }
}
