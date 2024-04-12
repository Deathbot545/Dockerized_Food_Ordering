namespace Food_Ordering_Web.DTO
{
    public class OutletInfoDTO
    {
        public string CustomerFacingName { get; set; }
        public byte[] Logo { get; set; }
        public byte[] RestaurantImage { get; set; }
        public TimeSpan OperatingHoursStart { get; set; }
        public TimeSpan OperatingHoursEnd { get; set; }
        public string Contact { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
    }
}
