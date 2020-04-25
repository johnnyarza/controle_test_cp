package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;

import application.dao.MaterialDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.Material;
import application.service.ProviderService;

public class MaterialDaoJDBC implements MaterialDao {

	private Connection conn;

	public MaterialDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(Material obj) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("INSERT INTO materials " + "(name, " + "providerId) " + "VALUES " + "(? ,?)",
					Statement.RETURN_GENERATED_KEYS);

			st.setString(1, obj.getName());
			st.setInt(2, obj.getProvider().getId());

			int rowsAffected = st.executeUpdate();

			if (rowsAffected > 0) {
				rs = st.getGeneratedKeys();
				if (rs.next()) {
					int id = rs.getInt(1);
					obj.setId(id);
				}
			} else {
				throw new DbException("No rows affected");
			}

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}
	}

	@Override
	public void update(Material obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("UPDATE materials SET" + "(name = ?, " + "providerId = ?) " + "WHERE id = ? ");

			st.setString(1, obj.getName());
			st.setInt(2, obj.getProvider().getId());
			st.setInt(3, obj.getId());

			int rowsAffected = st.executeUpdate();

			if (rowsAffected == 0) {

				throw new DbException("No rows affected");
			}

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
	}

	@Override
	public void deleteById(Integer id) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("DELETE FROM materials WHERE id = ?");

			st.setInt(1, id);

			int rowsAffected = st.executeUpdate();

			if (rowsAffected == 0) {

				throw new DbException("No rows affected");
			}

		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
	}

	@Override
	public Material findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		Material obj = new Material();
		try {
			st = conn.prepareStatement(
					"SELECT * FROM materials WHERE id = ?");
			
			st.setInt(1, id);
			
			rs = st.executeQuery();
			ProviderService service = new ProviderService();
			
			if (rs.next()) {
				obj.setId(rs.getInt(1));
				obj.setName(rs.getString(2));
				obj.setProvider(service.findById(rs.getInt(3)));
			}
			return obj;			
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
			if (rs != null)
			DB.closeResultSet(rs);
		}
	}

	@Override
	public List<Material> findAll() {
		PreparedStatement st = null;
		ResultSet rs = null;
		List<Material>  list = new ArrayList<>();
		try {
			st = conn.prepareStatement(
					"SELECT * FROM materials");
			
			rs = st.executeQuery();
			ProviderService service = new ProviderService();
		
			while (rs.next()) {
				Material obj = new Material();
				obj.setId(rs.getInt(1));
				obj.setName(rs.getString(2));
				obj.setProvider(service.findById(rs.getInt(3)));
				list.add(obj);
			}
			return list;			
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}
	}

	@Override
	public List<Material> findByDiffrentId(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		List<Material>  list = new ArrayList<>();
		try {
			st = conn.prepareStatement(
					"SELECT * FROM materials WHERE id <> ?");
			st.setInt(1, id);
			
			rs = st.executeQuery();
			ProviderService service = new ProviderService();
		
			while (rs.next()) {
				Material obj = new Material();
				obj.setId(rs.getInt(1));
				obj.setName(rs.getString(2));
				obj.setProvider(service.findById(rs.getInt(3)));
				list.add(obj);
			}
			return list;			
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}
	}

}
