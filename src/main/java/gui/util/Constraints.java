package gui.util;

import java.text.DecimalFormat;
import java.text.DecimalFormatSymbols;

import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
import javafx.scene.control.TextField;

public class Constraints {

	public static void setTextFieldInteger(TextField txt) {
		ChangeListener<String> setTextFieldInteger = new ChangeListener<>() {
			@Override
			public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) {
				if (newValue != null && !newValue.matches("\\d*")) {
					txt.setText(oldValue);
				}
			}
		};

		txt.textProperty().addListener(setTextFieldInteger);
		// txt.textProperty().removeListener(setTextFieldInteger);
	}

	public static void setTextFieldMaxLength(TextField txt, int max) {
		txt.textProperty().addListener((obs, oldValue, newValue) -> {
			if (newValue != null && newValue.length() > max) {
				txt.setText(oldValue);
			}
		});
	}

	public static void setTextFieldDouble(TextField txt) {
		DecimalFormat format = (DecimalFormat) DecimalFormat.getInstance();
		DecimalFormatSymbols symbols = format.getDecimalFormatSymbols();
		char sep = symbols.getDecimalSeparator();
		txt.textProperty().addListener((obs, oldValue, newValue) -> {
			if (newValue != null && !newValue.matches("\\d*([\\" + sep + "]\\d*)?")) {
				txt.setText(oldValue);
			}
		});
	}

	public static void setTextFieldIPV4(TextField txt) {
		String regex = makePartialIPRegex();
		txt.textProperty().addListener((obs, oldValue, newValue) -> {
			if (newValue != null && !newValue.matches(regex)) {
				txt.setText(oldValue);
			}
		});
	}

	public static String makePartialIPRegex() {
		String partialBlock = "(([01]?[0-9]{0,2})|(2[0-4][0-9])|(25[0-5]))";
		String subsequentPartialBlock = "(\\." + partialBlock + ")";
		String ipAddress = partialBlock + "?" + subsequentPartialBlock + "{0,3}";
		return "^" + ipAddress;
	}
}