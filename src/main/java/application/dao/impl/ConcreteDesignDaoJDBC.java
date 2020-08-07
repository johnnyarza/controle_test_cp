package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.sql.Types;
import java.util.ArrayList;
import java.util.List;

import application.dao.ConcreteDesignDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.ConcreteDesign;
import application.domaim.MaterialProporcion;
import application.service.MaterialService;

public class ConcreteDesignDaoJDBC implements ConcreteDesignDao {

	private Connection conn;

	public ConcreteDesignDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(ConcreteDesign obj) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement(
					"INSERT INTO concretedesign " + "(description, " + "fck, " + "mat1_id, " + "mat2_id, " + "mat3_id, "
							+ "mat4_id, " + "mat5_id, " + "mat6_id, " + "mat7_id, " + "mat8_id," + "mat1_qtt,"
							+ "mat2_qtt," + "mat3_qtt," + "mat4_qtt," + "mat5_qtt," + "mat6_qtt," + "mat7_qtt,"
							+ "mat8_qtt," + "slump" + ") " + "VALUES "
							+ "(? ,? ,? ,? ,? ,? ,? ,? ,? ,?,? ,? ,? ,? ,? ,? ,? ,?,?)",
					Statement.RETURN_GENERATED_KEYS);

			setStatement(st, obj);
			/*
			 * st.setString(1, obj.getDescription()); st.setDouble(2, obj.getFck());
			 * 
			 * st.setInt(3, obj.getProporcion().getMat1().getId()); st.setInt(4,
			 * obj.getProporcion().getMat2().getId()); st.setInt(5,
			 * obj.getProporcion().getMat3().getId()); st.setInt(6,
			 * obj.getProporcion().getMat4().getId()); st.setInt(7,
			 * obj.getProporcion().getMat5().getId()); st.setInt(8,
			 * obj.getProporcion().getMat6().getId()); st.setInt(9,
			 * obj.getProporcion().getMat7().getId());
			 * st.setInt(10,obj.getProporcion().getMat8().getId());
			 * 
			 * st.setDouble(11, obj.getProporcion().getMat1Qtt()); st.setDouble(12,
			 * obj.getProporcion().getMat2Qtt()); st.setDouble(13,
			 * obj.getProporcion().getMat3Qtt()); st.setDouble(14,
			 * obj.getProporcion().getMat4Qtt()); st.setDouble(15,
			 * obj.getProporcion().getMat5Qtt()); st.setDouble(16,
			 * obj.getProporcion().getMat6Qtt()); st.setDouble(17,
			 * obj.getProporcion().getMat7Qtt()); st.setDouble(18,
			 * obj.getProporcion().getMat8Qtt());
			 */

			int rows = st.executeUpdate();
			if (rows > 0) {
				rs = st.getGeneratedKeys();
				if (rs.next()) {
					obj.setId(rs.getInt(1));
				}
			} else {
				throw new DbException("No rows were affected");
			}

		} catch (SQLException e) {
			throw new DbException("Error inserting ConcreteDesing");
		} finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}
	}

	@Override
	public void update(ConcreteDesign obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement(
					"UPDATE concretedesign SET " + "description = ?, " + "fck = ?, " + "mat1_id = ?, " + "mat2_id = ?, "
							+ "mat3_id = ?, " + "mat4_id = ?, " + "mat5_id = ?, " + "mat6_id = ?, " + "mat7_id = ?, "
							+ "mat8_id = ?," + "mat1_qtt = ?," + "mat2_qtt = ?," + "mat3_qtt = ?," + "mat4_qtt = ?,"
							+ "mat5_qtt = ?," + "mat6_qtt = ?," + "mat7_qtt = ?," + "mat8_qtt = ?," +"slump = ? "+ "WHERE id = ?"  ,
					Statement.RETURN_GENERATED_KEYS);

			setStatement(st, obj);
			st.setInt(20, obj.getId());

			int rows = st.executeUpdate();
			if (rows == 0) {
				throw new DbException("No rows were affected");
			}

		} catch (SQLException e) {
			throw new DbException("Error inserting ConcreteDesing");
		} finally {
			DB.closeStatement(st);
		}
	}

	private void setStatement(PreparedStatement st, ConcreteDesign obj) throws SQLException {
		st.setString(1, obj.getDescription());
		st.setDouble(2, obj.getFck());

		MaterialProporcion prop = obj.getProporcion();

		st.setString(1, obj.getDescription());
		st.setDouble(2, obj.getFck());

		if (prop.getMat1() == null || prop.getMat1().isAllNull()) {
			st.setNull(3, Types.INTEGER);
		} else {
			st.setInt(3, prop.getMat1().getId());
		}

		if (prop.getMat2() == null || prop.getMat2().isAllNull()) {
			st.setNull(4, Types.INTEGER);
		} else {
			st.setInt(4, prop.getMat2().getId());
		}

		if (prop.getMat3() == null || prop.getMat3().isAllNull()) {
			st.setNull(5, Types.INTEGER);
		} else {
			st.setInt(5, prop.getMat3().getId());
		}

		if (prop.getMat4() == null || prop.getMat4().isAllNull()) {
			st.setNull(6, Types.INTEGER);
		} else {
			st.setInt(6, prop.getMat4().getId());
		}

		if (prop.getMat5() == null || prop.getMat5().isAllNull()) {
			st.setNull(7, Types.INTEGER);
		} else {
			st.setInt(7, prop.getMat5().getId());
		}

		if (prop.getMat6() == null || prop.getMat6().isAllNull()) {
			st.setNull(8, Types.INTEGER);
		} else {
			st.setInt(8, prop.getMat6().getId());
		}

		if (prop.getMat7() == null || prop.getMat7().isAllNull()) {
			st.setNull(9, Types.INTEGER);
		} else {
			st.setInt(9, prop.getMat7().getId());
		}

		if (prop.getMat8() == null || prop.getMat8().isAllNull()) {
			st.setNull(10, Types.INTEGER);
		} else {
			st.setInt(10, prop.getMat8().getId());
		}

		st.setDouble(11, prop.getMat1Qtt());
		st.setDouble(12, prop.getMat2Qtt());
		st.setDouble(13, prop.getMat3Qtt());
		st.setDouble(14, prop.getMat4Qtt());
		st.setDouble(15, prop.getMat5Qtt());
		st.setDouble(16, prop.getMat6Qtt());
		st.setDouble(17, prop.getMat7Qtt());
		st.setDouble(18, prop.getMat8Qtt());

		st.setDouble(19, obj.getSlump());

	}

	@Override
	public void deleteById(Integer id) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("DELETE FROM concretedesign " + "WHERE id = ?");

			st.setInt(1, id);
			int rows = st.executeUpdate();
			if (rows == 0) {
				throw new DbException("No rows were deleted");
			}
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}

	}

	@Override
	public ConcreteDesign findById(Integer id) {
		ResultSet rs = null;
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("SELECT * FROM concretedesign WHERE id = ?");
			st.setInt(1, id);

			rs = st.executeQuery();

			ConcreteDesign obj = new ConcreteDesign();

			if (rs.next()) {
				obj = instantiateConcreteDesign(rs);
			}
			return obj;
		} catch (SQLException e) {
			throw new DbException("Error finding All! " + e.getMessage());
		} finally {
			DB.closeResultSet(rs);
			DB.closeStatement(st);
		}
	}

	@Override
	public List<ConcreteDesign> findAll() {
		ResultSet rs = null;
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("SELECT * FROM concretedesign");

			rs = st.executeQuery();

			List<ConcreteDesign> list = new ArrayList<>();

			while (rs.next()) {
				ConcreteDesign obj = instantiateConcreteDesign(rs);
				list.add(obj);
			}
			return list;
		} catch (SQLException e) {
			throw new DbException("Error finding All! " + e.getMessage());
		} finally {
			DB.closeResultSet(rs);
			DB.closeStatement(st);
		}
	}

	private ConcreteDesign instantiateConcreteDesign(ResultSet rs) throws SQLException {
		ConcreteDesign obj = new ConcreteDesign();
		MaterialProporcion matProp = new MaterialProporcion();
		MaterialService service = new MaterialService();

		obj.setId(rs.getInt(1));
		obj.setDescription(rs.getString(2));

		obj.setFck(rs.getDouble(3));

		matProp.setMat1(service.findById(rs.getInt(4)));
		matProp.setMat2(service.findById(rs.getInt(5)));
		matProp.setMat3(service.findById(rs.getInt(6)));
		matProp.setMat4(service.findById(rs.getInt(7)));
		matProp.setMat5(service.findById(rs.getInt(8)));
		matProp.setMat6(service.findById(rs.getInt(9)));
		matProp.setMat7(service.findById(rs.getInt(10)));
		matProp.setMat8(service.findById(rs.getInt(11)));

		matProp.setMat1Qtt(rs.getDouble(12));
		matProp.setMat2Qtt(rs.getDouble(13));
		matProp.setMat3Qtt(rs.getDouble(14));
		matProp.setMat4Qtt(rs.getDouble(15));
		matProp.setMat5Qtt(rs.getDouble(16));
		matProp.setMat6Qtt(rs.getDouble(17));
		matProp.setMat7Qtt(rs.getDouble(18));
		matProp.setMat8Qtt(rs.getDouble(19));
		
		obj.setSlump(rs.getDouble(20));

		obj.setProporcion(matProp);

		return obj;
	}
}
