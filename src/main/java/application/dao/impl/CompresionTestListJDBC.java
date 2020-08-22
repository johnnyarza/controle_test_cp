package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;
import java.util.TimeZone;

import application.dao.CompresionTestListDao;
import application.db.DB;
import application.domaim.CompresionTestList;
import gui.util.Alerts;
import javafx.scene.control.Alert.AlertType;

public class CompresionTestListJDBC implements CompresionTestListDao{
	
	private Connection conn;
	
	public CompresionTestListJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public List<CompresionTestList> findAll() {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement(
					"SELECT " 
					+ "compresion_test.id, "
					+ "clients.name, " 
					+ "compresion_test.obra, "
					+ "compresion_test.address, "
					+ "compresion_test.creacionDate " 
					+ ",clients.id "
					+ "FROM cp_db.compresion_test "
					+ "INNER JOIN cp_db.clients "
					+ " ON compresion_test.client_id = clients.id " 
					+ "ORDER BY compresion_test.creacionDate");
			
			rs = st.executeQuery();
			
			Calendar cal = Calendar.getInstance(TimeZone.getDefault());
			List<CompresionTestList> list = new ArrayList<>();
			while (rs.next()) {
				list.add(new CompresionTestList(
						rs.getInt(1),
						rs.getInt(6),
						rs.getString(2), 
						rs.getString(3), 
						rs.getString(4), 
						new java.util.Date(rs.getTimestamp(5,cal).getTime())
						));
			}
			return list;			
		} catch (SQLException e) {
			Alerts.showAlert("Error", "Error loading CompresionTestList", e.getMessage(), AlertType.ERROR);
		} finally {
			DB.closeResultSet(rs);
			DB.closeStatement(st);
		}
		return null;
	}

}
