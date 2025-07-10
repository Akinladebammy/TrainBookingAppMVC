namespace TrainBookingAppMVC.DTOs.ResponseModel
{
    public class UserListResponseModel
    {
        public List<UserResponseModel> Users { get; set; } = new List<UserResponseModel>();
        public int TotalCount { get; set; }
    }
}
