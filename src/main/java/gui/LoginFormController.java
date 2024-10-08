package gui;

import java.net.URL;
import java.util.HashMap;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.logging.Level;

import javax.security.auth.login.LoginException;

import application.db.DbException;
import application.domaim.User;
import application.exceptions.ValidationException;
import application.log.LogUtils;
import application.service.UserService;
import enums.LogEnum;
import gui.util.Alerts;
import gui.util.Utils;
import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
import javafx.scene.control.TextField;

public class LoginFormController implements Initializable {

	private LogEnum logOption = LogEnum.SIGNIN;

	private UserService userService;

	private User entity;

	private LogUtils logger;

	@FXML
	private Button okBtn;

	@FXML
	private Button cancelBtn;

	@FXML
	private TextField txtUser;

	@FXML
	private PasswordField pwField;

	@FXML
	private Label userErrLabel;

	@FXML
	private Label passwordErrLabel;
	
	@FXML
	private Label titleLabel;

	@FXML
	private void onBtOkAction(ActionEvent event) {
		try {
			setErrorMessages(new HashMap<String, String>());
			setEntity(getUserFromForm());
			if (logOption == LogEnum.SIGNIN) {
				setEntity(userService.findByNameAndPassword(entity));
				if (entity == null) {
					throw new LoginException("Usuario o contraseña inválidos");
				}
			}
			if (logOption == LogEnum.SIGNUP ||logOption == LogEnum.EDIT ) {
				if (entity == null) {
					throw new IllegalStateException("User was null");
				}
				userService.saveOrInsert(entity);
				Alerts.showAlert("Aviso", "Alteraciones guardadas", null, AlertType.INFORMATION);
			}
			
			Utils.currentStage(event).close();
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (LoginException e) {
			Alerts.showAlert("Error", "Credenciales inválidas", e.getMessage(), AlertType.ERROR);
		} catch (DbException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "DbException", e.getMessage(), AlertType.ERROR);
		} catch (Exception e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error Desconocído", e.getMessage(), AlertType.ERROR);
		}

	}

	@FXML
	private void onBtCloseAction(ActionEvent event) {
		setEntity(null);
		Utils.currentStage(event).close();
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> errSet = errors.keySet();
		userErrLabel.setText(errSet.contains("user") ? "vacío" : "");
		passwordErrLabel.setText(errSet.contains("password") ? "vacío" : "");
	}

	private User getUserFromForm() {
		User user = new User();
		if (entity != null) {
			user.setId(entity.getId());
		}
		
		ValidationException exception = new ValidationException("Validation Error");
		if (txtUser.getText().trim().equals("") || txtUser.getText() == null) {
			exception.addError("user", "vacío");
		}

		user.setName(txtUser.getText());

		if (pwField.getText().trim().equals("") || pwField.getText() == null) {
			exception.addError("password", "vacío");
		}
		user.setPassword(pwField.getText());
		user.setRole("adm");
		
		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		return user;
	}
	
	public void setTitleLabel(String msg) {
		titleLabel.setText(msg);
	}
	
	public void setTxtUserText(String msg) {
		txtUser.setText(msg);
	}

	@Override
	public void initialize(URL location, ResourceBundle resources) {
		// TODO Auto-generated method stub

	}

	public void setUserService(UserService userService) {
		this.userService = userService;
	}

	public User getEntity() {
		return entity;
	}

	public void setEntity(User entity) {
		this.entity = entity;
	}

	public LogEnum getIsLoggin() {
		return logOption;
	}

	public void setIsLoggin(LogEnum logOption) {
		this.logOption = logOption;
	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

}
