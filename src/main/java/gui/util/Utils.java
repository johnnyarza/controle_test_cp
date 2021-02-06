package gui.util;

import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.sql.SQLException;
import java.text.DecimalFormat;
import java.text.NumberFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.time.temporal.ChronoUnit;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.TimeZone;
import java.util.function.Consumer;
import java.util.logging.Level;

import application.domaim.CorpoDeProva;
import application.log.LogUtils;
import application.service.MigrationService;
import application.service.UserService;
import enums.LogEnum;
import gui.CompresionTestFormController;
import gui.LoginFormController;
import javafx.event.ActionEvent;
import javafx.event.EventHandler;
import javafx.fxml.FXMLLoader;
import javafx.scene.Node;
import javafx.scene.Scene;
import javafx.scene.control.Button;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Dialog;
import javafx.scene.control.MenuBar;
import javafx.scene.control.TableCell;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableRow;
import javafx.scene.control.TableView;
import javafx.scene.control.TextInputDialog;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;
import javafx.scene.layout.AnchorPane;
import javafx.stage.FileChooser;
import javafx.stage.Modality;
import javafx.stage.FileChooser.ExtensionFilter;
import javafx.stage.Stage;
import javafx.stage.WindowEvent;
import javafx.util.StringConverter;

public class Utils {

	private static NumberFormat format = NumberFormat.getInstance(Locale.getDefault());
	private static DecimalFormat df = (DecimalFormat) format;

	public static String getStringWithDialog(String title, String header, Image image) {
		Dialog<String> dlg = new TextInputDialog();
		dlg.setTitle(title);
		dlg.setHeaderText(header);
		Stage stage = (Stage) dlg.getDialogPane().getScene().getWindow();
		stage.getIcons().add(image);
		return dlg.showAndWait().orElse("");
	}

	public static Stage currentStage(ActionEvent event) {
		return (Stage) ((Node) event.getSource()).getScene().getWindow();
	}

	public static Stage getMenuBarStage(MenuBar myMenuBar) {
		return (Stage) myMenuBar.getScene().getWindow();
	}

	public static Integer tryParseToInt(String str) {
		try {
			return Integer.parseInt(str);
		} catch (NumberFormatException e) {
			return null;
		}
	}

	public static Double tryParseToDouble(String str) {
		try {
			Number number = format.parse(str);
			return number.doubleValue();
		} catch (NumberFormatException | ParseException e) {
			return null;
		}
	}

	public static void formatDatePicker(DatePicker datePicker, String format) {
		datePicker.setConverter(new StringConverter<LocalDate>() {
			DateTimeFormatter dateFormatter = DateTimeFormatter.ofPattern(format);
			{
				datePicker.setPromptText(format.toLowerCase());
			}

			@Override
			public String toString(LocalDate date) {
				if (date != null) {
					return dateFormatter.format(date);
				} else {
					return "";
				}
			}

			@Override
			public LocalDate fromString(String string) {
				if (string != null && !string.isEmpty()) {
					return LocalDate.parse(string, dateFormatter);
				} else {
					return null;
				}
			}
		});
	}

	public static <T> void formatTableColumnDate(TableColumn<T, Date> tableColumn, String format) {
		tableColumn.setCellFactory(column -> {
			TableCell<T, Date> cell = new TableCell<T, Date>() {
				SimpleDateFormat sdf = new SimpleDateFormat(format);

				@Override
				protected void updateItem(Date item, boolean empty) {
					super.updateItem(item, empty);
					if (empty) {
						setText(null);
					} else {
						sdf.setTimeZone(TimeZone.getDefault());
						setText(sdf.format(item));
					}
				}
			};
			return cell;
		});
	}

	public static <T> void formatTableColumnDouble(TableColumn<T, Double> tableColumn, int decimalPlaces) {
		tableColumn.setCellFactory(column -> {
			TableCell<T, Double> cell = new TableCell<T, Double>() {
				@Override
				protected void updateItem(Double item, boolean empty) {
					super.updateItem(item, empty);
					if (empty) {
						setText(null);
					} else {
						String strDecimalPlaces = ".";
						for (int i = 1; i <= decimalPlaces; i++) {
							strDecimalPlaces = strDecimalPlaces + "0";
						}
						df.applyPattern("#,###" + strDecimalPlaces);
						if (item != null) {
							setText(df.format(item));
						}
					}
				}
			};
			return cell;
		});
	}

	public static String doubleFormat(Double number) {
		df.applyPattern("#.00");
		return df.format(number);
	}

	public static void formatCorpoDeProvaTableViewRowColor(TableView<CorpoDeProva> tableView) {
		tableView.setRowFactory(row -> new TableRow<CorpoDeProva>() {
			@Override
			public void updateItem(CorpoDeProva item, boolean empty) {
				super.updateItem(item, empty);

				if (item == null || empty) {
					setStyle("");
				} else {
					if (daysBetweenDates(item.getRuptureDate(), new Date()) == 0
							&& (item.getTonRupture() == null || item.getTonRupture() == 0f)) {
						getStyleClass().add("today-row");
						getStyleClass().add("table-row-cell");

						// setStyle("-fx-background-color: #f1c40f");
					} else if (item.getRuptureDate().compareTo(new Date()) < 0
							&& (item.getTonRupture() == null || item.getTonRupture() == 0f)) {
						getStyleClass().add("late-row");
						getStyleClass().add("table-row-cell");
						// setStyle("-fx-background-color: #e74c3c");
					} else if (daysBetweenDates(item.getRuptureDate(), new Date()) == 1
							&& (item.getTonRupture() == null || item.getTonRupture() == 0f)) {
						// setStyle("-fx-background-color: #2ecc71");
						getStyleClass().add("table-row-cell");
						getStyleClass().add("tomorrow-row");

					} else {
						setStyle("");
					}
				}
			}
		});
	}

	// In this method is necessary that the Scene already has a css file.
	public static <T> void formatTableViewRows(TableView<T> tableView, List<String> cssClasses) {
		if (tableView.getParent().getScene().getStylesheets().size() == 0) {
			throw new IllegalStateException("The Scene does not has a CSS file definition");
		}
		tableView.setRowFactory(row -> new TableRow<T>() {

			@Override
			public void updateItem(T item, boolean empty) {
				super.updateItem(item, empty);

				if (item == null || empty) {
					setStyle("");
				} else {
					for (String cssClass : cssClasses) {
						getStyleClass().add(cssClass);
					}
				}

			}
		});
	}

	public static Instant getInsTantFromDatePicker(DatePicker dP) {
		Instant instant = Instant.from(dP.getValue().atStartOfDay(ZoneId.systemDefault()));
		return instant;
	}

	public static Integer daysBetweenDates(Date futureDate, Date pastDay) {
		int days = (Math
				.abs((int) ChronoUnit.DAYS.between(futureDate.toInstant().atZone(ZoneId.systemDefault()).toLocalDate(),
						pastDay.toInstant().atZone(ZoneId.systemDefault()).toLocalDate())));
		return days;
	}

	public static Integer daysBetweenLocalDates(LocalDate futureDate, LocalDate pastDay) {
		int days = Math.abs((int) ChronoUnit.DAYS.between(futureDate, pastDay));
		return days;
	}

	public static ImageView createImageView(String path, Double height, Double width) {
		Image image = new Image(Utils.class.getResourceAsStream(path));
		ImageView imgView = new ImageView(image);
		if (height != null && height != 0 && width != null && width != 0) {
			imgView.setFitHeight(height);
			imgView.setFitWidth(width);
		}
		return imgView;
	}

	public static Image createImage(String absolutePath) throws FileNotFoundException {
		File file = new File(absolutePath);
		if (file == null || !(file.isFile())) {
			throw new FileNotFoundException(
					"Error al Cargar Imagen(es). Es posible que la(s) imagene(s) no exista(n)!");
		}
		Image image = new Image(file.toURI().toString());
		return image;
	}

	public static String getFileAbsolutePath(Stage dialogParentStage, ExtensionFilter filter)
			throws FileNotFoundException {
		FileChooser fileChooser = new FileChooser();
		fileChooser.getExtensionFilters().addAll(filter);
		File file = fileChooser.showOpenDialog(dialogParentStage);
		if (file == null || !(file.isFile())) {
			throw new FileNotFoundException("Error al Cargar Imagen");
		}
		return file.getAbsolutePath();
	}

	public static void setButtonGraphic(String path, Button button, Double height, Double width) {
		ImageView imgView = Utils.createImageView(path, height, width);
		button.setGraphic(imgView);
	}

	public static <T> void createDialogForm(String absoluteName, String title, Stage parentStage,
			Consumer<T> initializingAction, Consumer<T> windowEventAction, Consumer<T> finalAction, String css,
			Image icon, LogUtils logger) {
		try {

			FXMLLoader loader = new FXMLLoader(Utils.class.getResource(absoluteName));
			AnchorPane pane = loader.load();

			T controller = loader.getController();
			initializingAction.accept(controller);

			Stage dialogStage = new Stage();

			dialogStage.getIcons().add(icon);
			dialogStage.setTitle(title);
			dialogStage.setScene(new Scene(pane));
			dialogStage.getScene().getStylesheets().add("/gui/GlobalStyle.css");
			if (!css.trim().equals("")) {
				dialogStage.getScene().getStylesheets().add(css);
			}
			dialogStage.setResizable(true);
			dialogStage.initOwner(parentStage);
			dialogStage.initModality(Modality.WINDOW_MODAL);

			dialogStage.setOnCloseRequest(new EventHandler<WindowEvent>() {

				@Override
				public void handle(WindowEvent we) {
					windowEventAction.accept(controller);

				}
			});

			dialogStage.showAndWait();
			finalAction.accept(controller);

		} catch (IOException e) {
			logger.doLog(Level.WARNING, e.getMessage(), e);
			Alerts.showAlert("Error", "Error al crear ventana", "IOException", AlertType.ERROR);
		}
	}

	public static boolean isUserAdmin(ActionEvent event, LogUtils logger) throws SQLException {

		Stage parentStage = Utils.currentStage(event);
		var wrapper = new Object() {
			public UserService userService;
			private boolean isUserAdmin = true;

			public void setIsUserAdmin(boolean bool) {
				isUserAdmin = bool;
			};

			public boolean getIsUserAdmin() {
				return isUserAdmin;
			}
		};
		wrapper.userService = new UserService();

		Consumer<LoginFormController> initialCons = (LoginFormController loginFormController) -> {
			loginFormController.setUserService(wrapper.userService);
			loginFormController.setEntity(null);
			loginFormController.setIsLoggin(LogEnum.SIGNIN);
			loginFormController.setLogger(logger);
			loginFormController.setTitleLabel("Entrar con datos del administrador");
		};

		Consumer<LoginFormController> windowEventCons = (LoginFormController loginFormController) -> {
			loginFormController.setEntity(null);
		};

		Consumer<LoginFormController> finalCons = (LoginFormController loginFormController) -> {
			wrapper.setIsUserAdmin((loginFormController.getEntity() == null ? true : false));
		};

		createDialogForm("/gui/LoginForm.fxml", "Login", parentStage, initialCons, windowEventCons, finalCons, "",
				new Image(CompresionTestFormController.class.getResourceAsStream("/images/sign_in.png")), logger);

		return wrapper.getIsUserAdmin();
	};

	private static void initiateTables() throws SQLException {
		MigrationService service = new MigrationService();
		try {
			service.initiateDB();
		} catch (Exception e) {
			Alerts.showAlert("Error", "Error al iniciar el DB", e.getMessage(), AlertType.ERROR);
		}

	}

}