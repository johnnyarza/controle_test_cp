package gui;

import java.io.IOException;
import java.net.URL;
import java.util.HashMap;
import java.util.Map;
import java.util.Optional;
import java.util.Properties;
import java.util.ResourceBundle;
import java.util.Set;
import java.util.logging.Level;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import application.exceptions.ValidationException;
import application.log.LogUtils;
import application.util.EncriptaDecriptaApacheCodec;
import application.util.FileUtils;
import gui.util.Alerts;
import gui.util.Constraints;
import javafx.application.Platform;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonType;
import javafx.scene.control.Label;
import javafx.scene.control.PasswordField;
import javafx.scene.control.TextField;

public class ConnectionConfigViewController implements Initializable {

	private LogUtils logger;

	@FXML
	private TextField txtIp;

	@FXML
	private TextField txtPort;

	@FXML
	private TextField txtUser;

	@FXML
	private PasswordField pfPassword;

	@FXML
	private Button btSave;
	
	@FXML
	private Label ipLabel;

	@FXML
	private Label portLabel;

	@FXML
	private Label userLabel;

	@FXML
	private Label passLabel;
	

	
	@FXML
	public void onBtSaveAction() {

		try {

			Optional<ButtonType> option = (Alerts.showConfirmationDialog("Aviso", "Seguro que desea continuar?",
					"El programa será cerrado para aplicar las configuraciones"));
			if (option.get() != ButtonType.OK) {
				return;
			}
			Map<String, String> configs = getViewData();

			FileUtils.writeConnectionProperties(configs);
			Alerts.showAlert("Confimarción", "Configuraciones guardadas!", "", AlertType.INFORMATION);
			Platform.exit();
		} catch (ValidationException e) {
			setErrorMessages(e.getErrors());
		} catch (IOException e1) {
			logger.doLog(Level.WARNING, e1.getMessage(), e1);
			Alerts.showAlert("Error", "IOException", e1.getMessage(), AlertType.ERROR);
		}
	}

	private void setErrorMessages(Map<String, String> errors) {
		Set<String> errorsKey = errors.keySet();
		ipLabel.setText(errorsKey.contains("ip") ? errors.get("ip") : "");
		portLabel.setText(errorsKey.contains("port") ? errors.get("port") : "");
		userLabel.setText(errorsKey.contains("user") ? errors.get("user") : "");
		passLabel.setText(errorsKey.contains("pass") ? errors.get("pass") : "");

	}

	private Map<String, String> getViewData() {
		Map<String, String> configs = new HashMap<>();
		ValidationException exception = new ValidationException("Validation error");
		String dburl;

		if (txtIp.getText() != null && txtIp.getText().trim().equals("")) {
			exception.addError("ip", "vacío");
		}

		if (txtPort.getText() != null && txtPort.getText().trim().equals("")) {
			exception.addError("port", "vacío");
		}
		dburl = "jdbc:mysql://" + txtIp.getText() + ":" + txtPort.getText() + "/?useTimezone=true&serverTimezone=UTC";
		configs.put("dburl", dburl);

		if (txtUser.getText() != null && txtUser.getText().trim().equals("")) {
			exception.addError("user", "vacío");
		}
		configs.put("user", txtUser.getText());

		if (pfPassword.getText() != null && pfPassword.getText().trim().equals("")) {
			exception.addError("pass", "vacío");
		}
		configs.put("password", EncriptaDecriptaApacheCodec.codificaBase64Encoder(pfPassword.getText()));

		if (exception.getErrors().size() > 0) {
			throw exception;
		}

		return configs;
	}

	private void initializeNodes() {
		Constraints.setTextFieldInteger(txtPort);
		Constraints.setTextFieldIPV4(txtIp);
	}

	public void updateViewData() {
		try {
			Properties props = FileUtils.loadConnetionProperties();
			txtIp.setText(props.getProperty("dburl").trim().equals("") ? "" : getIpFromDbUrl(props.getProperty("dburl")));
			txtPort.setText(
					props.getProperty("dburl").trim().equals("") ? "" : getPortFromProperties(props.getProperty("dburl")));
			txtUser.setText(props.getProperty("user").trim().equals("") ? "" : props.getProperty("user"));
		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "IOException", e.getMessage(), AlertType.ERROR);
		}
	}

	private String getIpFromDbUrl(String dburl) {
		String old = dburl;
		String ip;
		Integer index;
		old = old.replace("jdbc:mysql://", "");
		old = old.replace("/cp_db?useTimezone=true&serverTimezone=UTC", "");
		index = old.indexOf(":");
		ip = old.substring(0, index);
		if (ip.matches(Constraints.makePartialIPRegex())) {
			ip = ip.replaceAll("\\b(\\d{3})(\\d{3})(\\d{3})(\\d{3})\\b", "$1.$2.$3.$4");
		}
		;
		return ip;
	}

	private String getPortFromProperties(String port) {
		String prt = "";
		Pattern pattern = Pattern.compile("(\\:{1})\\d{1,5}");
		Matcher matcher = pattern.matcher(port);
		if (matcher.find()) {
			prt = matcher.group(0);
			prt = prt.replace(":", "");
		}
		return prt;
	}

	@Override
	public void initialize(URL location, ResourceBundle resources) {
		initializeNodes();

	}

	public void setLogger(LogUtils logger) {
		this.logger = logger;
	}

}
