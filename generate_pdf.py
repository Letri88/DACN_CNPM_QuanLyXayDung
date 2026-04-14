from fpdf import FPDF
import os

class SimpleContractPDF(FPDF):
    def header(self):
        self.set_font('Arial', 'B', 16)
        self.cell(0, 10, 'HOP DONG XAY DUNG MAU', 0, 1, 'C')
        self.ln(10)

def create_pdf():
    pdf = SimpleContractPDF()
    
    # Tim font Arial tren Windows de ho tro Unicode (co dau)
    font_path = r'C:\Windows\Fonts\arial.ttf'
    if os.path.exists(font_path):
        pdf.add_font('ArialUni', '', font_path)
        pdf.set_font('ArialUni', '', 12)
    else:
        # Fallback neu khong tim thay font (se bi loi font neu co dau)
        pdf.set_font('Helvetica', '', 12)
    
    pdf.add_page()
    
    content = [
        "Tổng chi phí dự án: 500.000.000 VNĐ",
        "Mã dự án: 101",
        "",
        "CHI TIẾT CÁC GIAI ĐOẠN:",
        "--------------------------------",
        "Tên giai đoạn: Thiết kế và San lấp",
        "Mã nhân viên: EN001",
        "Chi phí giai đoạn: 50.000.000 VNĐ",
        "Bắt đầu: 15/04/2026",
        "Kết thúc: 17/04/2026",
        "",
        "Tên giai đoạn: Thi công móng",
        "Mã nhân viên: EN001",
        "Chi phí: 150.000.000 VNĐ",
        "Bắt đầu: 17/04/2026",
        "Kết thúc: 20/04/2026",
        "",
        "Tên giai đoạn: Hoàn thiện",
        "Mã nhân viên: EN001",
        "Chi phí giai đoạn: 100.000.000 VNĐ",
        "Bắt đầu: 20/04/2026",
        "Kết thúc: 30/04/2026",
        "",
        "--------------------------------",
        "Các bên ký tên xác nhận."
    ]
    
    for line in content:
        pdf.cell(0, 10, line, new_x="LMARGIN", new_y="NEXT")
        
    output_path = r"E:\DACN_CNPM_QuanLyXayDung\HopDongMau_Test.pdf"
    pdf.output(output_path)
    print(f"Đã tạo file PDF có dấu tại: {os.path.abspath(output_path)}")

if __name__ == "__main__":
    create_pdf()
