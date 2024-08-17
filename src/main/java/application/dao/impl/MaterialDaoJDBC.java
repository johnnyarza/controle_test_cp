package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Level;

import application.dao.MaterialDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.Material;
import application.log.LogUtils;
import application.service.ProviderService;
import gui.util.Alerts;
import javafx.scene.control.Alert.AlertType;

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
			if (conn.getAutoCommit())
				conn.setAutoCommit(false);
			st = conn.prepareStatement("INSERT INTO cp_db.materials " + "(name, " + "providerId) " + "VALUES " + "(? ,?)",
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
			conn.commit();
		} catch (SQLException e) {
			rollback();
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, rs);
		}
	}

	@Override
	public void update(Material obj) {
		PreparedStatement st = null;
		try {
			if (conn.getAutoCommit())
				conn.setAutoCommit(false);
			st = conn.prepareStatement("UPDATE cp_db.materials SET " + "name = ?, " + "providerId = ? " + "WHERE id = ? ");

			st.setString(1, obj.getName());
			st.setInt(2, obj.getProvider().getId());
			st.setInt(3, obj.getId());

			int rowsAffected = st.executeUpdate();

			if (rowsAffected == 0) {

				throw new DbException("No rows affected");
			}
			conn.commit();
		} catch (SQLException e) {
			rollback();
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, null);
		}
	}

	@Override
	public void deleteById(Integer id) {
		PreparedStatement st = null;
		try {
			if (conn.getAutoCommit())
				conn.setAutoCommit(false);
			st = conn.prepareStatement("DELETE FROM cp_db.materials WHERE id = ?");

			st.setInt(1, id);

			int rowsAffected = st.executeUpdate();

			if (rowsAffected == 0) {

				throw new DbException("No rows affected");
			}
			conn.commit();
		} catch (SQLException e) {
			rollback();
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, null);
		}
	}

	@Override
	public Material findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		Material obj = new Material();
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.materials WHERE id = ?");

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
		List<Material> list = new ArrayList<>();
		try {
			st = conn.prepareStatement("SELECT * FROM materials");

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
	public List<Material> findByProviderId(Integer providerId) {
		PreparedStatement st = null;
		ResultSet rs = null;
		List<Material> list = new ArrayList<>();
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.materials where providerId=?");

			st.setInt(1, providerId);
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
		List<Material> list = new ArrayList<>();
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.materials WHERE id <> ?");
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

	private void finallyActions(PreparedStatement st, ResultSet rs) {
		try {
			if (!conn.getAutoCommit()) {
				conn.setAutoCommit(true);
			}
			if (st != null) {
				DB.closeStatement(st);
			}
			if (rs != null) {
				DB.closeResultSet(rs);
			}
		} catch (Exception e2) {
			Alerts.showAlert("Error", "Error desconocídos", e2.getMessage(), AlertType.ERROR);
			LogUtils logger = new LogUtils();
			logger.doLog(Level.WARNING, e2.getMessage(), e2);
		}
	}

	private void rollback() {
		try {
			conn.rollback();
		} catch (Exception e) {
			Alerts.showAlert("Error", "Error desconocídos", e.getMessage(), AlertType.ERROR);
			LogUtils logger = new LogUtils();
			logger.doLog(Level.WARNING, e.getMessage(), e);
		}
	}

}
