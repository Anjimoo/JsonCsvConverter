import React, { Component } from 'react';
import './TableRow.css';

export class TableRow extends Component {
    static displayName = TableRow.name;

    constructor(props) {
        super(props);
        this.state = {
            key: props.row.key,
            value: props.row.value,
            newValue: props.row.newValue
        }
    }

    componentWillReceiveProps(nextProps) {
        this.setState({
            key: nextProps.row.key,
            value: nextProps.row.value,
            newValue: nextProps.row.newValue
        });
    }

    render() {
        return (
            <tr key={this.state.key}>
                <td>{this.state.key}</td>
                <td>{this.state.value}</td>
                <td>
                    <input className='new-value-input' type='text'
                        onChange={e => this.props.handleUpdate(e, this.state.key)}
                        value={this.state.newValue}
                        onBlur={e => this.props.sendUpdatedTable(e, this.state.key)}
                    />
                </td>
            </tr>
        );
    }
}
