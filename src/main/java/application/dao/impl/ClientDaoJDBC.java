package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Level;

import application.dao.ClientDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.Cliente;
import application.log.LogUtils;
import gui.util.Alerts;
import javafx.scene.control.Alert.AlertType;

public class ClientDaoJDBC implements ClientDao {

	private Connection conn;

	public ClientDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(Cliente obj) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			conn.setAutoCommit(false);
			st = conn.prepareStatement("INSERT INTO cp_db.clients " + "(name,phone,address,email) " + "VALUES " + "(?,?,?,?)",
					Statement.RETURN_GENERATED_KEYS);

			st.setString(1, obj.getName());
			st.setString(2, obj.getPhone());
			st.setString(3, obj.getAddress());
			st.setString(4, obj.getEmail());

			int rowsAffected = st.executeUpdate();

			if (rowsAffected > 0) {
				rs = st.getGeneratedKeys();
				if (rs.next()) {
					int id = rs.getInt(1);
					obj.setId(id);
				}
				DB.closeResultSet(rs);
			} else {
				throw new DbException("Unexpected Error. No rows affected");
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
	public void update(Cliente obj) {
		PreparedStatement st = null;
		try {
			conn.setAutoCommit(false);
			st = conn.prepareStatement("update cp_db.clients set " + "clients.name = ?, " + "clients.phone =?, "
					+ "clients.address = ?, " + "clients.email = ? " + "WHERE clients.id = ?");

			st.setString(1, obj.getName());
			st.setString(2, obj.getPhone());
			st.setString(3, obj.getAddress());
			st.setString(4, obj.getEmail());
			st.setInt(5, obj.getId());

			st.executeUpdate();
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
			conn.setAutoCommit(false);

			st = conn.prepareStatement("delete from cp_db.clients where clients.id = ?");
			st.setInt(1, id);
			st.executeUpdate();

			conn.commit();
		} catch (SQLException e) {
			rollback();
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, null);
		}
	}

	@Override
	public Cliente findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.clients where clients.id = ?");
			st.setInt(1, id);
			rs = st.executeQuery();

			if (rs.next()) {
				return new Cliente(rs.getInt(1), rs.getString(2), rs.getString(3), rs.getString(4), rs.getString(5));
			}

			return null;
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, rs);
		}
	}

	@Override
	public List<Cliente> findAll() {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.clients");

			rs = st.executeQuery();

			List<Cliente> list = new ArrayList<>();
			while (rs.next()) {
				list.add(new Cliente(rs.getInt(1), rs.getString(2), rs.getString(3), rs.getString(4), rs.getString(5)));
			}
			return list;
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, rs);
		}
	}

	@Override
	public List<Cliente> findByLikeName(String str) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.clients WHERE name LIKE '%" + str + "%'");

			rs = st.executeQuery();

			List<Cliente> list = new ArrayList<>();
			while (rs.next()) {
				list.add(new Cliente(rs.getInt(1), rs.getString(2), rs.getString(3), rs.getString(4), rs.getString(5)));
			}
			return list;
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, rs);
		}
	}

	@Override
	public Cliente findByName(String name) {

		PreparedStatement st = null;
		ResultSet rs = null;

		try {
			st = conn.prepareStatement("SELECT * FROM cp_db.clients where clients.name = ?");

			st.setString(1, name);
			rs = st.executeQuery();

			rs = st.executeQuery();

			if (rs.next()) {
				return new Cliente(rs.getInt(1), rs.getString(2), rs.getString(3), rs.getString(4), rs.getString(5));
			}
			return null;
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			finallyActions(st, rs);
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
