using MySql.Data.MySqlClient;
using System.Data;

namespace webChamcong.Services
{
    public class ChamCongService
    {
        private readonly string _connectionString;

        public ChamCongService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // =========================================================================
        // 1. HÀM ĐĂNG NHẬP
        // =========================================================================
        public int Login(string phone, string password)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // Kiểm tra SĐT, Mật khẩu và Trạng thái Active
                string sql = @"SELECT nv.id 
                               FROM nhan_vien nv 
                               JOIN tai_khoan tk ON nv.tai_khoan_id = tk.id 
                               WHERE nv.so_dien_thoai = @Phone 
                               AND tk.mat_khau_hash = @Pass 
                               AND tk.trang_thai = 'active'";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@Pass", password);
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
        }

        // =========================================================================
        // 2. HÀM CHẤM CÔNG (Check In / Check Out)
        // =========================================================================
        public string CheckInOrOut(int employeeId, bool isCheckOut)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // --- BƯỚC 1: LẤY GIỜ BẮT ĐẦU CỦA CA LÀM VIỆC ---
                string getCaSql = "SELECT id, gio_bat_dau, gio_ket_thuc FROM ca_lam WHERE ngay_lam = CURDATE() LIMIT 1";
                int caId = 0;
                TimeSpan gioBatDauCa = TimeSpan.Zero;

                using (var cmdCa = new MySqlCommand(getCaSql, conn))
                {
                    using (var reader = cmdCa.ExecuteReader())
                    {
                        if (!reader.Read()) return "❌ Lỗi: Hôm nay công ty chưa tạo ca làm việc!";

                        caId = reader.GetInt32("id");
                        if (reader["gio_bat_dau"] != DBNull.Value)
                        {
                            gioBatDauCa = (TimeSpan)reader["gio_bat_dau"];
                        }
                    }
                }

                // --- BƯỚC 1.5: KIỂM TRA PHÂN CA (CHẶN NGƯỜI KHÔNG CÓ LỊCH) ---
                string checkPhanCa = "SELECT COUNT(*) FROM phan_cong_ca WHERE nhan_vien_id = @EmpId AND ca_id = @CaId";
                using (var cmdPhanCa = new MySqlCommand(checkPhanCa, conn))
                {
                    cmdPhanCa.Parameters.AddWithValue("@EmpId", employeeId);
                    cmdPhanCa.Parameters.AddWithValue("@CaId", caId);
                    long count = (long)cmdPhanCa.ExecuteScalar();

                    if (count == 0)
                    {
                        return "⛔ Bạn không được phân công làm việc trong ca này! Vui lòng liên hệ quản lý.";
                    }
                }

                // --- BƯỚC 2: KIỂM TRA TRẠNG THÁI HIỆN TẠI (ĐÃ CHECK-IN CHƯA?) ---
                string checkSql = "SELECT id, gio_vao, gio_ra, trang_thai FROM cham_cong WHERE nhan_vien_id = @EmpId AND ca_id = @CaId";
                int ccId = 0;
                TimeSpan? gioRa = null;
                string trangThaiCu = "";

                using (var cmdCheck = new MySqlCommand(checkSql, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@EmpId", employeeId);
                    cmdCheck.Parameters.AddWithValue("@CaId", caId);
                    using (var reader = cmdCheck.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ccId = reader.GetInt32("id");
                            if (reader["gio_ra"] != DBNull.Value) gioRa = Convert.ToDateTime(reader["gio_ra"]).TimeOfDay;
                            trangThaiCu = reader["trang_thai"].ToString();
                        }
                    }
                }

                // --- BƯỚC 3: XỬ LÝ LOGIC CHECK-IN / CHECK-OUT ---
                TimeSpan now = DateTime.Now.TimeOfDay;

                if (!isCheckOut)
                {
                    // === NGƯỜI DÙNG BẤM NÚT CHECK IN ===
                    if (ccId != 0) return "⚠️ Bạn đã Check-in hôm nay rồi!";

                    string newStatus = "checked_in";
                    string msg = "✅ Check-in thành công (Đúng giờ)!";

                    // Logic tính Đi Muộn (Trễ quá 15 phút)
                    if (now > gioBatDauCa.Add(TimeSpan.FromMinutes(15)))
                    {
                        newStatus = "late";
                        msg = $"⚠️ Bạn đã đi muộn! (Ca bắt đầu lúc: {gioBatDauCa})";
                    }

                    string insertSql = "INSERT INTO cham_cong (nhan_vien_id, ca_id, gio_vao, trang_thai) VALUES (@EmpId, @CaId, NOW(), @Status)";
                    using (var cmdIn = new MySqlCommand(insertSql, conn))
                    {
                        cmdIn.Parameters.AddWithValue("@EmpId", employeeId);
                        cmdIn.Parameters.AddWithValue("@CaId", caId);
                        cmdIn.Parameters.AddWithValue("@Status", newStatus);
                        cmdIn.ExecuteNonQuery();
                    }
                    return msg;
                }
                else
                {
                    // === NGƯỜI DÙNG BẤM NÚT CHECK OUT ===
                    if (ccId == 0) return "⚠️ Bạn chưa Check-in nên không thể Check-out!";
                    if (gioRa != null) return "⚠️ Bạn đã Check-out rồi!";

                    string updateStatus = "checked_out";
                    string updateSql = "UPDATE cham_cong SET gio_ra = NOW(), trang_thai = @Status WHERE id = @CcId";
                    using (var cmdOut = new MySqlCommand(updateSql, conn))
                    {
                        cmdOut.Parameters.AddWithValue("@Status", updateStatus);
                        cmdOut.Parameters.AddWithValue("@CcId", ccId);
                        cmdOut.ExecuteNonQuery();
                    }
                    return "✅ Check-out thành công!";
                }
            }
        }
    }
}