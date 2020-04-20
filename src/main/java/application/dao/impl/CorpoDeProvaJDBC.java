package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeUnit;

import application.dao.CorpoDeProvaDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.CompresionTest;
import application.domaim.CorpoDeProva;
import application.service.CompresionTestService;

public class CorpoDeProvaJDBC implements CorpoDeProvaDao{
	
	private Connection conn;
	
	
	public CorpoDeProvaJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(CorpoDeProva obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("INSERT INTO corpo_de_provas " + 
					"(" + 
			/*1*/	"code, " + 	
			/*2*/	"client_id, " + 
			/*3*/	"compresionTest_Id, " + 
			/*4*/	"slump, " + 
			/*5*/	"dateMolde, " + 
			/*6*/	"ruptureDate, " + 
			/*7*/	"age, " + 
			/*8*/	"diameter, " + 
			/*9*/	"height, " + 
			/*10*/	"weight, " + 
			/*11*/	"tonRupture) " + 
				"VALUES (? ,? , ? ,? ,? ,? ,? ,? ,? ,? ,? )",
					Statement.RETURN_GENERATED_KEYS);
			
			st.setString(1, obj.getCode());
			st.setInt(2, obj.getCompresionTest().getClient().getId());
			st.setInt(3, obj.getCompresionTest().getId());
			st.setDouble(4, obj.getSlump());
			st.setDate(5, new java.sql.Date(obj.getDataMolde().getTime()));
			st.setDate(6, new java.sql.Date(obj.getRuptureDate().getTime()));
			st.setInt(7, (int) TimeUnit.DAYS.convert((obj.getRuptureDate().getTime() - obj.getDataMolde().getTime()),TimeUnit.MILLISECONDS));
			st.setDouble(8, obj.getDiameter());
			st.setDouble(9, obj.getHeight());
			st.setDouble(10, obj.getWeight());
			st.setDouble(11, obj.getTonRupture());
			
			int rowsAffected = st.executeUpdate();
			
			if (rowsAffected > 0) {
				ResultSet rs = st.getGeneratedKeys();
				if (rs.next()) {
					int id = rs.getInt(1);
					obj.setId(id);
				}
				DB.closeResultSet(rs);
			} else {
				throw new DbException("Unexpected Error. No rows affected");
			}
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}		
	}

	@Override
	public void update(CorpoDeProva obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("UPDATE corpo_de_provas SET "
					+ "corpo_de_provas.code = ?, " //1
					+ "client_id = ?, " //2
					+ "compresionTest_Id = ?, " //3
					+ "slump = ?, " //4
					+ "dateMolde = ?, " //5
					+ "ruptureDate = ?, " //6
					+ "age = ?, " //7
					+ "diameter = ?, " //8
					+ "height = ?, " //9
					+ "weight = ?, " //10
					+ "tonRupture = ? " //11
					+ "WHERE id = ?)"); //12
			
			st.setString(1, obj.getCode());
			st.setInt(2, obj.getCompresionTest().getClient().getId());
			st.setInt(3, obj.getCompresionTest().getId());
			st.setDouble(4, obj.getSlump());
			st.setDate(5, new java.sql.Date(obj.getDataMolde().getTime()));
			st.setDate(6, new java.sql.Date(obj.getRuptureDate().getTime()));
			st.setInt(7, (int) TimeUnit.DAYS.convert((obj.getRuptureDate().getTime()-obj.getDataMolde().getTime()),TimeUnit.MILLISECONDS));
			st.setDouble(8, obj.getDiameter());
			st.setDouble(9, obj.getHeight());
			st.setDouble(10, obj.getWeight());
			st.setDouble(11, obj.getTonRupture());
			st.setInt(12, obj.getId());
			
			st.executeUpdate();

		} 
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		} 
		finally {
			DB.closeStatement(st);
		}	
	}

	@Override
	public void deleteById(Integer id) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("delete from corpo_de_provas where id = ?");
			
			st.setInt(1, id);
			
			st.executeUpdate();

		} 
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		} 
		finally {
			DB.closeStatement(st);
		}	
	}

	@Override
	public CorpoDeProva findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM corpo_de_provas where id = ?");
			
			st.setInt(1, id);
			rs = st.executeQuery();
			
			if (rs.next()) {
				CorpoDeProva obj = instantiateCorpoDeProva(rs); 
				obj.setDensid();
				obj.setFckRupture();
				obj.setDays(rs.getInt(8));
				return obj;
			}
			return null;
		}
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
		finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}	
	}
	
	@Override
	public List<CorpoDeProva> findAll() {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM corpo_de_provas");
			
			rs = st.executeQuery();
			
			List<CorpoDeProva> list = new ArrayList<>();
			while (rs.next()) {			
				CorpoDeProva obj = instantiateCorpoDeProva(rs); 
				obj.setDensid();
				obj.setFckRupture();
				obj.setDays(rs.getInt(8));
				list.add(obj);
			}
			return list;
		}
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
		finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}	
	}

	@Override
	public List<CorpoDeProva> findByCompresionTestId(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM corpo_de_provas WHERE "
					+ "compresionTest_Id = ?");
			
			st.setInt(1, id);
			
			rs = st.executeQuery();
			
			List<CorpoDeProva> list = new ArrayList<>();
			while (rs.next()) {	
				CorpoDeProva obj = instantiateCorpoDeProva(rs); 
				obj.setDensid();
				obj.setFckRupture();
				obj.setDays(rs.getInt(8));
				list.add(obj);
			}
			return list;
		}
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
		finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}
	}
	private CorpoDeProva instantiateCorpoDeProva(ResultSet rs) throws SQLException {
		CompresionTestService compresionTestService = new CompresionTestService();
		
		CompresionTest compresionTest = compresionTestService.findById(rs.getInt(4));
		CorpoDeProva obj = new CorpoDeProva(rs.getInt(1), 
				rs.getString(2),
				compresionTest, 
				rs.getDouble(5), 
				new java.util.Date(rs.getTimestamp(6).getTime()), 
				new java.util.Date(rs.getTimestamp(7).getTime()), 
				rs.getDouble(9), 
				rs.getDouble(10), 
				rs.getDouble(11), 
				rs.getDouble(12));

		return obj;
	}
}
